using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Application.Abstractions.Security;
using Application.Models;
using Application.Messaging.Commands;
using Application.UseCases.Jobs.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.UseCases.Jobs;

/// <summary>
/// Password change job olusturma ve update komutlarini kuyruga gonderme akisidir.
/// </summary>
public sealed class CreatePasswordChangeJobUseCase
{
    private readonly IServerGroupRepository _serverGroupRepository;
    private readonly IJobRepository _jobRepository;
    private readonly ICommandPublisher _commandPublisher;
    private readonly IPayloadProtector _payloadProtector;

    public CreatePasswordChangeJobUseCase(
        IServerGroupRepository serverGroupRepository,
        IJobRepository jobRepository,
        ICommandPublisher commandPublisher,
        IPayloadProtector payloadProtector)
    {
        _serverGroupRepository = serverGroupRepository;
        _jobRepository = jobRepository;
        _commandPublisher = commandPublisher;
        _payloadProtector = payloadProtector;
    }

    /// <summary>
    /// Password change job olusturur, hedefleri kaydeder ve update komutlarini publish eder.
    /// </summary>
    public async Task<UseCaseResult<JobCreatedOutput>> ExecuteAsync(
        CreatePasswordChangeJobInput input,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.ServerGroupExternalId))
        {
            return UseCaseResult<JobCreatedOutput>.Failure("SERVER_GROUP_REQUIRED", "Server group zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(input.RequestedBy))
        {
            return UseCaseResult<JobCreatedOutput>.Failure("REQUESTED_BY_REQUIRED", "RequestedBy zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(input.TargetAccount))
        {
            return UseCaseResult<JobCreatedOutput>.Failure("TARGET_ACCOUNT_REQUIRED", "TargetAccount zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(input.OldPassword) || string.IsNullOrWhiteSpace(input.NewPassword))
        {
            return UseCaseResult<JobCreatedOutput>.Failure("PASSWORD_REQUIRED", "Old ve new password zorunludur.");
        }

        var serverGroup = await _serverGroupRepository.GetByExternalIdAsync(
            input.ServerGroupExternalId,
            cancellationToken);

        if (serverGroup is null)
        {
            return UseCaseResult<JobCreatedOutput>.Failure("SERVER_GROUP_NOT_FOUND", "Server group bulunamadi.");
        }

        var servers = await _serverGroupRepository.GetServersAsync(serverGroup.Id, cancellationToken);
        if (servers.Count == 0)
        {
            return UseCaseResult<JobCreatedOutput>.Failure("SERVER_GROUP_EMPTY", "Server group icinde hedef yok.");
        }

        var job = new Job
        {
            Id = Guid.NewGuid(),
            Type = JobType.PasswordChange,
            Status = JobStatus.Pending,
            RequestedBy = input.RequestedBy,
            ServerGroupId = serverGroup.Id,
            CorrelationId = input.CorrelationId
        };

        foreach (var server in servers)
        {
            job.Targets.Add(new JobTarget
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                ServerName = server.Hostname,
                IpAddress = server.IpAddress,
                Status = TargetStatus.Pending
            });
        }

        await _jobRepository.AddAsync(job, cancellationToken);
        await _jobRepository.SaveChangesAsync(cancellationToken);

        // Yeni sifreyi MQ payload'i icin AES-GCM ile sifreler (plain sifre log/DB/MQ'ya yazilmaz).
        var encryptedPayload = await _payloadProtector.EncryptAsync(input.NewPassword, cancellationToken);

        foreach (var target in job.Targets)
        {
            // Update komutunu MQ'ya gonderir.
            var command = new UpdateServerResourcesCommand
            {
                JobId = job.Id,
                TargetId = target.Id,
                ServerName = target.ServerName,
                IpAddress = target.IpAddress,
                EncryptedPassword = encryptedPayload,
                CorrelationId = input.CorrelationId
            };

            await _commandPublisher.PublishUpdateAsync(command, cancellationToken);
        }

        return UseCaseResult<JobCreatedOutput>.Success(new JobCreatedOutput
        {
            JobId = job.Id,
            Status = job.Status.ToString(),
            CreatedAt = job.CreatedAt,
            TargetCount = job.Targets.Count
        });
    }
}
