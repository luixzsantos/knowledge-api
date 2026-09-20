namespace SecondBrain.Domain.Entities;

// Representa algo que o usuário está aprendendo (ex: "Redis", "Goroutines").
// É o núcleo do dicionário técnico pessoal — as demais entidades (Note, Project, Tag)
// vão se relacionar com Concept nas próximas versões.
public class Concept
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ConceptLevel? Level { get; set; }

    // Conteúdo estruturado do verbete (em vez de um bloco de texto único):
    // cada campo responde uma pergunta específica de quem está aprendendo.
    public string? SimpleAnalogy { get; set; } // "em termos simples", sem jargão
    public string? Explanation { get; set; } // como funciona / como usar
    public string? CodeExample { get; set; } // um exemplo real
    public string? ExpectedOutput { get; set; } // o que o exemplo produz — ou, se nada é impresso, o que está acontecendo
    public string? WhereUsed { get; set; } // onde esse código pode ser usado na prática
    public string? DocumentationUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ConceptNote> ConceptNotes { get; set; } = [];
    public ICollection<ConceptProject> ConceptProjects { get; set; } = [];
    public ICollection<ConceptTag> ConceptTags { get; set; } = [];

    // Auto-relacionamento (grafo de conhecimento): "Redis" relacionado a "Redis Streams",
    // "Redis" alternativa a "RabbitMQ" etc. Duas coleções porque é uma relação em si mesma —
    // ver ConceptRelation.
    public ICollection<ConceptRelation> RelationsAsSource { get; set; } = [];
    public ICollection<ConceptRelation> RelationsAsTarget { get; set; } = [];
}
