using SecondBrain.Domain.Entities;

namespace SecondBrain.Application.DTOs;

// Um outro Concept relacionado a este — o grafo de conhecimento. RelationType vem
// como texto (JsonStringEnumConverter global). Ver ConceptRelation.
public record ConceptRelationDto(Guid ConceptId, string ConceptName, ConceptRelationType RelationType);

// Visão completa de um Concept — "página do conceito": o que ele é + tudo que já
// foi relacionado a ele (ver visão do produto no README: notas, projetos, tags e
// outros conceitos relacionados).
public record ConceptDetailDto(
    Guid Id,
    string Name,
    string? Description,
    ConceptLevel? Level,
    string? SimpleAnalogy,
    string? Explanation,
    string? CodeExample,
    string? ExpectedOutput,
    string? WhereUsed,
    string? DocumentationUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<NoteDto> Notes,
    List<ProjectDto> Projects,
    List<TagDto> Tags,
    List<ConceptRelationDto> Relations
);
