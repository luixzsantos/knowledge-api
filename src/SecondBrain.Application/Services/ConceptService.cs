using Microsoft.Extensions.Logging;
using SecondBrain.Application.DTOs;
using SecondBrain.Application.Exceptions;
using SecondBrain.Application.Interfaces;
using SecondBrain.Domain.Entities;

namespace SecondBrain.Application.Services;

public class ConceptService(
    IConceptRepository repository,
    INoteRepository noteRepository,
    IProjectRepository projectRepository,
    ITagRepository tagRepository,
    ILogger<ConceptService> logger) : IConceptService
{
    public async Task<List<ConceptDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var concepts = await repository.GetAllAsync(cancellationToken);
        return concepts.Select(ToDto).ToList();
    }

    public async Task<List<ConceptDto>> GetAllByTagAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        _ = await tagRepository.GetByIdAsync(tagId, cancellationToken)
            ?? throw new NotFoundException($"Tag '{tagId}' não encontrada.");

        var concepts = await repository.GetAllByTagAsync(tagId, cancellationToken);
        return concepts.Select(ToDto).ToList();
    }

    public async Task<ConceptDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var concept = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Concept '{id}' não encontrado.");

        return ToDto(concept);
    }

    public async Task<ConceptDetailDto> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var concept = await repository.GetByIdWithRelationsAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Concept '{id}' não encontrado.");

        var notes = concept.ConceptNotes
            .Select(cn => new NoteDto(cn.Note.Id, cn.Note.Title, cn.Note.Content, cn.Note.CreatedAt, cn.Note.UpdatedAt))
            .ToList();

        var projects = concept.ConceptProjects
            .Select(cp => new ProjectDto(cp.Project.Id, cp.Project.Name, cp.Project.Description, cp.Project.Status, cp.Project.CreatedAt, cp.Project.UpdatedAt))
            .ToList();

        var tags = concept.ConceptTags
            .Select(ct => new TagDto(ct.Tag.Id, ct.Tag.Name))
            .ToList();

        var relations = concept.RelationsAsSource
            .Select(r => new ConceptRelationDto(r.TargetConcept.Id, r.TargetConcept.Name, r.Type))
            .Concat(concept.RelationsAsTarget
                .Select(r => new ConceptRelationDto(r.SourceConcept.Id, r.SourceConcept.Name, r.Type)))
            .OrderBy(r => r.ConceptName)
            .ToList();

        return new ConceptDetailDto(
            concept.Id, concept.Name, concept.Description, concept.Level,
            concept.SimpleAnalogy, concept.Explanation, concept.CodeExample, concept.ExpectedOutput,
            concept.WhereUsed, concept.DocumentationUrl,
            concept.CreatedAt, concept.UpdatedAt,
            notes, projects, tags, relations);
    }

    public async Task LinkNoteAsync(Guid conceptId, Guid noteId, CancellationToken cancellationToken = default)
    {
        _ = await repository.GetByIdAsync(conceptId, cancellationToken)
            ?? throw new NotFoundException($"Concept '{conceptId}' não encontrado.");
        _ = await noteRepository.GetByIdAsync(noteId, cancellationToken)
            ?? throw new NotFoundException($"Note '{noteId}' não encontrada.");

        if (await repository.NoteLinkExistsAsync(conceptId, noteId, cancellationToken))
        {
            throw new ConflictException("Esse Concept já está relacionado a essa Note.");
        }

        await repository.AddNoteLinkAsync(conceptId, noteId, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlinkNoteAsync(Guid conceptId, Guid noteId, CancellationToken cancellationToken = default)
    {
        var removed = await repository.RemoveNoteLinkAsync(conceptId, noteId, cancellationToken);
        if (!removed)
        {
            throw new NotFoundException("Relação entre esse Concept e essa Note não encontrada.");
        }

        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task LinkProjectAsync(Guid conceptId, Guid projectId, CancellationToken cancellationToken = default)
    {
        _ = await repository.GetByIdAsync(conceptId, cancellationToken)
            ?? throw new NotFoundException($"Concept '{conceptId}' não encontrado.");
        _ = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException($"Project '{projectId}' não encontrado.");

        if (await repository.ProjectLinkExistsAsync(conceptId, projectId, cancellationToken))
        {
            throw new ConflictException("Esse Concept já está relacionado a esse Project.");
        }

        await repository.AddProjectLinkAsync(conceptId, projectId, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlinkProjectAsync(Guid conceptId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var removed = await repository.RemoveProjectLinkAsync(conceptId, projectId, cancellationToken);
        if (!removed)
        {
            throw new NotFoundException("Relação entre esse Concept e esse Project não encontrada.");
        }

        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task LinkTagAsync(Guid conceptId, Guid tagId, CancellationToken cancellationToken = default)
    {
        _ = await repository.GetByIdAsync(conceptId, cancellationToken)
            ?? throw new NotFoundException($"Concept '{conceptId}' não encontrado.");
        _ = await tagRepository.GetByIdAsync(tagId, cancellationToken)
            ?? throw new NotFoundException($"Tag '{tagId}' não encontrada.");

        if (await repository.TagLinkExistsAsync(conceptId, tagId, cancellationToken))
        {
            throw new ConflictException("Esse Concept já está relacionado a essa Tag.");
        }

        await repository.AddTagLinkAsync(conceptId, tagId, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlinkTagAsync(Guid conceptId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var removed = await repository.RemoveTagLinkAsync(conceptId, tagId, cancellationToken);
        if (!removed)
        {
            throw new NotFoundException("Relação entre esse Concept e essa Tag não encontrada.");
        }

        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task LinkRelationAsync(Guid conceptId, Guid relatedConceptId, ConceptRelationType type, CancellationToken cancellationToken = default)
    {
        if (conceptId == relatedConceptId)
        {
            throw new ConflictException("Um Concept não pode se relacionar com ele mesmo.");
        }

        _ = await repository.GetByIdAsync(conceptId, cancellationToken)
            ?? throw new NotFoundException($"Concept '{conceptId}' não encontrado.");
        _ = await repository.GetByIdAsync(relatedConceptId, cancellationToken)
            ?? throw new NotFoundException($"Concept '{relatedConceptId}' não encontrado.");

        if (await repository.RelationExistsAsync(conceptId, relatedConceptId, type, cancellationToken))
        {
            throw new ConflictException("Essa relação já existe.");
        }

        await repository.AddRelationAsync(conceptId, relatedConceptId, type, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlinkRelationAsync(Guid conceptId, Guid relatedConceptId, ConceptRelationType type, CancellationToken cancellationToken = default)
    {
        var removed = await repository.RemoveRelationAsync(conceptId, relatedConceptId, type, cancellationToken);
        if (!removed)
        {
            throw new NotFoundException("Essa relação não foi encontrada.");
        }

        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<ConceptDto> CreateAsync(CreateConceptRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByNameAsync(request.Name, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"Já existe um concept com o nome '{request.Name}'.");
        }

        var now = DateTime.UtcNow;
        var concept = new Concept
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Level = request.Level,
            SimpleAnalogy = request.SimpleAnalogy?.Trim(),
            Explanation = request.Explanation?.Trim(),
            CodeExample = request.CodeExample?.Trim(),
            ExpectedOutput = request.ExpectedOutput?.Trim(),
            WhereUsed = request.WhereUsed?.Trim(),
            DocumentationUrl = request.DocumentationUrl?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        await repository.AddAsync(concept, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Concept criado: {ConceptId} ({ConceptName})", concept.Id, concept.Name);

        return ToDto(concept);
    }

    public async Task<ConceptDto> UpdateAsync(Guid id, UpdateConceptRequest request, CancellationToken cancellationToken = default)
    {
        var concept = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Concept '{id}' não encontrado.");

        var existing = await repository.GetByNameAsync(request.Name, cancellationToken);
        if (existing is not null && existing.Id != id)
        {
            throw new ConflictException($"Já existe um concept com o nome '{request.Name}'.");
        }

        concept.Name = request.Name.Trim();
        concept.Description = request.Description?.Trim();
        concept.Level = request.Level;
        concept.SimpleAnalogy = request.SimpleAnalogy?.Trim();
        concept.Explanation = request.Explanation?.Trim();
        concept.CodeExample = request.CodeExample?.Trim();
        concept.ExpectedOutput = request.ExpectedOutput?.Trim();
        concept.WhereUsed = request.WhereUsed?.Trim();
        concept.DocumentationUrl = request.DocumentationUrl?.Trim();
        concept.UpdatedAt = DateTime.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Concept atualizado: {ConceptId}", concept.Id);

        return ToDto(concept);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var concept = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Concept '{id}' não encontrado.");

        repository.Remove(concept);
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Concept removido: {ConceptId}", id);
    }

    private static ConceptDto ToDto(Concept concept) => new(
        concept.Id,
        concept.Name,
        concept.Description,
        concept.Level,
        concept.CreatedAt,
        concept.UpdatedAt
    );
}
