namespace StandardDataPipeline.Source;

public interface IDataModule<T>
{
    string DataType { get; }
    T CreateDefault();
    T Merge(T current, T incoming);
    DataValidationResult Validate(T value);
}

public interface IDataProvider<T>
{
    string Name { get; }
    Task<DataProviderResult<T>> LoadAsync(CancellationToken cancellationToken);
}