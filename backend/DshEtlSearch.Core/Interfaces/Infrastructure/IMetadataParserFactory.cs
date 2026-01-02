using DshEtlSearch.Core.Common.Enums;

namespace DshEtlSearch.Core.Interfaces.Infrastructure;

public interface IMetadataParserFactory
{
    IMetadataParser GetParser(MetadataFormat format);
}