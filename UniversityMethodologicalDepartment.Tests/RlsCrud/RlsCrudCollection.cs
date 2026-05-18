namespace UniversityMethodologicalDepartment.Tests.RlsCrud;

/// <summary>
/// Коллекция xUnit: общая фикстура для интеграционных тестов RLS (последовательное выполнение, один cleanup).
/// </summary>
[CollectionDefinition("RlsCrud")]
public sealed class RlsCrudCollection : ICollectionFixture<RlsCrudCollectionFixture>;
