using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Application.Models;
using Application.Messaging.Commands;
using Application.UseCases.Jobs.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.UseCases.Jobs;

/// <summary>
/// Discovery job olusturma ve discovery komutlarini kuyruğa gonderme akisidir.
/// </summary>
public sealed class CreateDiscoveryJobUseCase
{
    private readonly IServerGroupRepository _serverGroupRepository;
    private readonly IJobRepository _jobRepository;
    private readonly ICommandPublisher _commandPublisher;

    public CreateDiscoveryJobUseCase(
        IServerGroupRepository serverGroupRepository,
        IJobRepository jobRepository,
        ICommandPublisher commandPublisher)
    {
        _serverGroupRepository = serverGroupRepository;
        _jobRepository = jobRepository;
        _commandPublisher = commandPublisher;
    }

    /// <summary>
    /// Discovery job olusturur, hedefleri kaydeder ve discovery komutlarini publish eder.
    /// </summary>
    public async Task<UseCaseResult<JobCreatedOutput>> ExecuteAsync(
        CreateDiscoveryJobInput input,
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
            Type = JobType.Discovery,
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

        foreach (var target in job.Targets)
        {
            // Discovery komutunu MQ'ya gonderir.
            var command = new DiscoverServerUsageCommand
            {
                JobId = job.Id,
                TargetId = target.Id,
                ServerName = target.ServerName,
                IpAddress = target.IpAddress,
                CorrelationId = input.CorrelationId
            };

            await _commandPublisher.PublishDiscoveryAsync(command, cancellationToken);
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
