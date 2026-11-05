// ReSharper disable UnusedMethodReturnValue.Global

using Application.Dtos;

namespace Application.Services;

public interface ICategoryService
{
    CategoryDTO AddMovie(CategoryDTO category);
    IList<CategoryDTO> GetAll();
    CategoryDTO FindById(int id);
    CategoryDTO Update(CategoryDTO updatedCategory);
    void Delete(int id);
}
