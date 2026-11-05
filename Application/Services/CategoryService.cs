using Application.Dtos;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;

internal class CategoryService(IRepository<Category> categoryRepository) : ICategoryService
{
    public CategoryDTO AddMovie(CategoryDTO category)
    {
        return CategoryDTO.fromEntity(categoryRepository.Add(category.toEntity()))!;
    }

    public IList<CategoryDTO> GetAll()
    {
        return categoryRepository.FindAll().Select(x => CategoryDTO.fromEntity(x)!).ToList();
    }

    public CategoryDTO FindById(int id)
    {
        return CategoryDTO.fromEntity(categoryRepository.Find(x => x.Id == id))!;
    }

    public CategoryDTO Update(CategoryDTO updatedCategory)
    {
        return CategoryDTO.fromEntity(categoryRepository.Update(updatedCategory.toEntity()))!;
    }

    public void Delete(int id)
    {
        categoryRepository.Delete(id);
    }
}
