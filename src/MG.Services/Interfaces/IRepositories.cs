using MG.Models.Entities;

namespace MG.Services.Interfaces;

public interface IDataRepository {
	Task<DataItem?> GetByIdAsync(string id);
	Task<DataItem> CreateAsync(DataItem dataItem);
	Task<DataItem?> UpdateAsync(DataItem dataItem);
	Task<bool> DeleteAsync(string id);
}

public interface IUserRepository {
	Task<User?> GetByEmailAsync(string email);
	Task<User> CreateAsync(User user);
}
