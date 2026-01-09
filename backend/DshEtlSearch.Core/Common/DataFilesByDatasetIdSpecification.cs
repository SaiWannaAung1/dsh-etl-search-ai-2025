using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces;
using System.Linq.Expressions;

namespace DshEtlSearch.Core.Common;

public class DataFilesByDatasetIdSpecification : BaseSpecification<DataFile>
{
    public Expression<Func<DataFile, bool>> Criteria { get; }
    public List<Expression<Func<DataFile, object>>> Includes { get; } = new();

    public DataFilesByDatasetIdSpecification(Guid datasetId)
    {
        // Links the DataFile to the Dataset via the Foreign Key
        Criteria = f => f.DatasetId == datasetId;
    }
}