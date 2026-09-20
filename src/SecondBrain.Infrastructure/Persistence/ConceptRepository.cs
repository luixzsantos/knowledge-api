using Microsoft.EntityFrameworkCore;
using SecondBrain.Application.Interfaces;
using SecondBrain.Domain.Entities;

namespace SecondBrain.Infrastructure.Persistence;

public class ConceptRepository(SecondBrainDbContext context) : IConceptRepository
{
    // Level vira string no banco (legivel em SQL direto), mas ordenar pela string
    // ordenaria "Avancado" antes de "Basico" - errado. O ternario inline (nao um
    // metodo separado - EF Core nao traduz chamada de metodo pro SQL) vira um CASE
    // no SQL, respeitando basico -> intermediario -> avancado -> sem nivel.
    public async Task<List<Concept>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Concepts
            .AsNoTracking()
            .OrderBy(c => c.Level == ConceptLevel.Basico ? 0 : c.Level == ConceptLevel.Intermediario ? 1 : c.Level == ConceptLevel.Avancado ? 2 : 3)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<List<Concept>> GetAllByTagAsync(Guid tagId, CancellationToken cancellationToken = default) =>
        await context.Concepts
            .AsNoTracking()
            .Where(c => c.ConceptTags.Any(ct => ct.TagId == tagId))
            .OrderBy(c => c.Level == ConceptLevel.Basico ? 0 : c.Level == ConceptLevel.Intermediario ? 1 : c.Level == ConceptLevel.Avancado ? 2 : 3)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<Concept?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Concepts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    // ToLower() dos dois lados continua aqui (mesmo Name sendo citext no Postgres)
    // porque os testes unitários rodam contra EF InMemory, que não conhece citext
    // e faria comparação case-sensitive sem isso. citext é quem garante a unicidade
    // de verdade no banco; esta consulta só precisa concordar com o mesmo critério.
    public async Task<Concept?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        await context.Concepts.FirstOrDefaultAsync(
            c => c.Name.ToLower() == name.Trim().ToLower(), cancellationToken);

    public async Task AddAsync(Concept concept, CancellationToken cancellationToken = default) =>
        await context.Concepts.AddAsync(concept, cancellationToken);

    public void Remove(Concept concept) => context.Concepts.Remove(concept);

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await context.SaveChangesAsync(cancellationToken) > 0;

    public async Task<Concept?> GetByIdWithRelationsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Concepts
            .Include(c => c.ConceptNotes).ThenInclude(cn => cn.Note)
            .Include(c => c.ConceptProjects).ThenInclude(cp => cp.Project)
            .Include(c => c.ConceptTags).ThenInclude(ct => ct.Tag)
            .Include(c => c.RelationsAsSource).ThenInclude(r => r.TargetConcept)
            .Include(c => c.RelationsAsTarget).ThenInclude(r => r.SourceConcept)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> NoteLinkExistsAsync(Guid conceptId, Guid noteId, CancellationToken cancellationToken = default) =>
        context.ConceptNotes.AnyAsync(cn => cn.ConceptId == conceptId && cn.NoteId == noteId, cancellationToken);

    public async Task AddNoteLinkAsync(Guid conceptId, Guid noteId, CancellationToken cancellationToken = default) =>
        await context.ConceptNotes.AddAsync(
            new ConceptNote { ConceptId = conceptId, NoteId = noteId, CreatedAt = DateTime.UtcNow }, cancellationToken);

    public async Task<bool> RemoveNoteLinkAsync(Guid conceptId, Guid noteId, CancellationToken cancellationToken = default)
    {
        var link = await context.ConceptNotes
            .FirstOrDefaultAsync(cn => cn.ConceptId == conceptId && cn.NoteId == noteId, cancellationToken);
        if (link is null)
        {
            return false;
        }

        context.ConceptNotes.Remove(link);
        return true;
    }

    public Task<bool> ProjectLinkExistsAsync(Guid conceptId, Guid projectId, CancellationToken cancellationToken = default) =>
        context.ConceptProjects.AnyAsync(cp => cp.ConceptId == conceptId && cp.ProjectId == projectId, cancellationToken);

    public async Task AddProjectLinkAsync(Guid conceptId, Guid projectId, CancellationToken cancellationToken = default) =>
        await context.ConceptProjects.AddAsync(
            new ConceptProject { ConceptId = conceptId, ProjectId = projectId, CreatedAt = DateTime.UtcNow }, cancellationToken);

    public async Task<bool> RemoveProjectLinkAsync(Guid conceptId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var link = await context.ConceptProjects
            .FirstOrDefaultAsync(cp => cp.ConceptId == conceptId && cp.ProjectId == projectId, cancellationToken);
        if (link is null)
        {
            return false;
        }

        context.ConceptProjects.Remove(link);
        return true;
    }

    public Task<bool> TagLinkExistsAsync(Guid conceptId, Guid tagId, CancellationToken cancellationToken = default) =>
        context.ConceptTags.AnyAsync(ct => ct.ConceptId == conceptId && ct.TagId == tagId, cancellationToken);

    public async Task AddTagLinkAsync(Guid conceptId, Guid tagId, CancellationToken cancellationToken = default) =>
        await context.ConceptTags.AddAsync(
            new ConceptTag { ConceptId = conceptId, TagId = tagId, CreatedAt = DateTime.UtcNow }, cancellationToken);

    public async Task<bool> RemoveTagLinkAsync(Guid conceptId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var link = await context.ConceptTags
            .FirstOrDefaultAsync(ct => ct.ConceptId == conceptId && ct.TagId == tagId, cancellationToken);
        if (link is null)
        {
            return false;
        }

        context.ConceptTags.Remove(link);
        return true;
    }

    public Task<bool> RelationExistsAsync(Guid conceptId, Guid relatedConceptId, ConceptRelationType type, CancellationToken cancellationToken = default) =>
        context.ConceptRelations.AnyAsync(r => r.Type == type &&
            ((r.SourceConceptId == conceptId && r.TargetConceptId == relatedConceptId) ||
             (r.SourceConceptId == relatedConceptId && r.TargetConceptId == conceptId)), cancellationToken);

    public async Task AddRelationAsync(Guid conceptId, Guid relatedConceptId, ConceptRelationType type, CancellationToken cancellationToken = default) =>
        await context.ConceptRelations.AddAsync(
            new ConceptRelation { SourceConceptId = conceptId, TargetConceptId = relatedConceptId, Type = type, CreatedAt = DateTime.UtcNow },
            cancellationToken);

    public async Task<bool> RemoveRelationAsync(Guid conceptId, Guid relatedConceptId, ConceptRelationType type, CancellationToken cancellationToken = default)
    {
        var relation = await context.ConceptRelations.FirstOrDefaultAsync(r => r.Type == type &&
            ((r.SourceConceptId == conceptId && r.TargetConceptId == relatedConceptId) ||
             (r.SourceConceptId == relatedConceptId && r.TargetConceptId == conceptId)), cancellationToken);
        if (relation is null)
        {
            return false;
        }

        context.ConceptRelations.Remove(relation);
        return true;
    }
}
