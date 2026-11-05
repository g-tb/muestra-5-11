// ReSharper disable InconsistentNaming

using Domain.Entities;

namespace Application.Dtos;

public class CategoryDTO
{
    public int Id { get; private init; }

    public string? Name { get; set; }
    
    public Category toEntity()
    {
        return new Category()
        {
            Id = this.Id,
            Name = this.Name,
        };
    }
    
    public static CategoryDTO? fromEntity(Category? category)
    {
        if (category is null)
        {
            return null;
        }
        
        return new CategoryDTO()
        {
            Id = category.Id,
            Name = category.Name,
        };
    }
}
