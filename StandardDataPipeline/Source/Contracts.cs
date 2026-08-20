namespace StandardDataPipeline.Source;

public readonly record struct DataPipelineContext(
    string RootDirectory);

public interface IDataDocument
{
    int DataVersion { get; }
}

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

    ValueTask<DataProviderResult<T>> LoadAsync(
        DataPipelineContext context,
        CancellationToken cancellationToken);
}