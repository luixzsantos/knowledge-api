using System.ComponentModel.DataAnnotations;
using SecondBrain.Domain.Entities;

namespace SecondBrain.Application.DTOs;

public record ConceptDto(
    Guid Id,
    string Name,
    string? Description,
    ConceptLevel? Level,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public class CreateConceptRequest
{
    [Required(ErrorMessage = "Name é obrigatório.")]
    [MaxLength(150, ErrorMessage = "Name deve ter no máximo 150 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(4000, ErrorMessage = "Description deve ter no máximo 4000 caracteres.")]
    public string? Description { get; set; }

    // Opcional: verbete criado a mao pela pessoa nao precisa classificar nivel.
    public ConceptLevel? Level { get; set; }

    // Conteudo estruturado (tudo opcional - um verbete criado a mao na UI nao
    // precisa preencher isso, so os importados de codigo real costumam ter).
    [MaxLength(2000, ErrorMessage = "SimpleAnalogy deve ter no máximo 2000 caracteres.")]
    public string? SimpleAnalogy { get; set; }

    [MaxLength(8000, ErrorMessage = "Explanation deve ter no máximo 8000 caracteres.")]
    public string? Explanation { get; set; }

    [MaxLength(8000, ErrorMessage = "CodeExample deve ter no máximo 8000 caracteres.")]
    public string? CodeExample { get; set; }

    [MaxLength(4000, ErrorMessage = "ExpectedOutput deve ter no máximo 4000 caracteres.")]
    public string? ExpectedOutput { get; set; }

    [MaxLength(2000, ErrorMessage = "WhereUsed deve ter no máximo 2000 caracteres.")]
    public string? WhereUsed { get; set; }

    [MaxLength(500, ErrorMessage = "DocumentationUrl deve ter no máximo 500 caracteres.")]
    public string? DocumentationUrl { get; set; }
}

public class UpdateConceptRequest
{
    [Required(ErrorMessage = "Name é obrigatório.")]
    [MaxLength(150, ErrorMessage = "Name deve ter no máximo 150 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(4000, ErrorMessage = "Description deve ter no máximo 4000 caracteres.")]
    public string? Description { get; set; }

    public ConceptLevel? Level { get; set; }

    [MaxLength(2000, ErrorMessage = "SimpleAnalogy deve ter no máximo 2000 caracteres.")]
    public string? SimpleAnalogy { get; set; }

    [MaxLength(8000, ErrorMessage = "Explanation deve ter no máximo 8000 caracteres.")]
    public string? Explanation { get; set; }

    [MaxLength(8000, ErrorMessage = "CodeExample deve ter no máximo 8000 caracteres.")]
    public string? CodeExample { get; set; }

    [MaxLength(4000, ErrorMessage = "ExpectedOutput deve ter no máximo 4000 caracteres.")]
    public string? ExpectedOutput { get; set; }

    [MaxLength(2000, ErrorMessage = "WhereUsed deve ter no máximo 2000 caracteres.")]
    public string? WhereUsed { get; set; }

    [MaxLength(500, ErrorMessage = "DocumentationUrl deve ter no máximo 500 caracteres.")]
    public string? DocumentationUrl { get; set; }
}

public class LinkConceptRelationRequest
{
    [Required(ErrorMessage = "Type é obrigatório.")]
    public ConceptRelationType Type { get; set; }
}
