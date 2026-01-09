using System.Xml.Linq;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Interfaces.Infrastructure;

namespace DshEtlSearch.Infrastructure.FileProcessing.Parsers.Strategies;

public class Iso19115XmlParser : IMetadataParser
{
    private static readonly XNamespace gmd = "http://www.isotc211.org/2005/gmd";
    private static readonly XNamespace gco = "http://www.isotc211.org/2005/gco";
    private static readonly XNamespace gmx = "http://www.isotc211.org/2005/gmx";

    public Result<ParsedMetadataDto> Parse(Stream content)
    {
        try
        {
            var doc = XDocument.Load(content);

            var title = doc.Descendants(gmd + "title").Descendants(gco + "CharacterString").FirstOrDefault()?.Value ?? "Untitled";
            var abstractText = doc.Descendants(gmd + "abstract").Descendants(gco + "CharacterString").FirstOrDefault()?.Value;

            // 2. FIXED: Targeted Author Extraction
            var affiliations = new List<AuthorAffiliation>();
            
            // Look for all ResponsibleParties in the whole document
            var allParties = doc.Descendants(gmd + "CI_ResponsibleParty");

            foreach (var party in allParties)
            {
                // Only process this party if the Role is "author"
                var role = party.Element(gmd + "role")?.Element(gmd + "CI_RoleCode")?.Attribute("codeListValue")?.Value;
                
                if (role == "author")
                {
                    var name = party.Descendants(gmd + "individualName")
                        .Descendants()
                        .FirstOrDefault(x => x.Name == gco + "CharacterString" || x.Name == gmx + "Anchor")?.Value;

                    var org = party.Descendants(gmd + "organisationName")
                        .Descendants()
                        .FirstOrDefault(x => x.Name == gco + "CharacterString" || x.Name == gmx + "Anchor")?.Value;

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        affiliations.Add(new AuthorAffiliation
                        {
                            Name = name.Trim(),
                            Organization = org?.Trim() ?? "Unknown Organization"
                        });
                    }
                }
            }

            // --- Keywords and URL Logic ---
            var keywords = doc.Descendants(gmd + "MD_TopicCategoryCode")
                              .Select(node => node.Value)
                              .Concat(doc.Descendants(gmd + "descriptiveKeywords").Descendants(gco + "CharacterString").Select(n => n.Value))
                              .Where(k => !string.IsNullOrEmpty(k))
                              .Distinct();

            var url = doc.Descendants(gmd + "CI_OnlineResource")
                         .Where(r => {
                             var func = r.Element(gmd + "function")?.Element(gmd + "CI_OnLineFunctionCode")?.Attribute("codeListValue")?.Value;
                             return func == "fileAccess" || func == "download";
                         })
                         .Descendants(gmd + "URL").FirstOrDefault()?.Value;
            
            var uniqueAuthors = affiliations
                .Where(a => !string.IsNullOrEmpty(a.Name) && a.Name != "N/A")
                .GroupBy(a => new { a.Name, a.Organization })
                .Select(g => g.First());

            string authorsString = string.Join(" / ", uniqueAuthors
                .Select(a => {
                    var name = System.Net.WebUtility.HtmlDecode(a.Name ?? "").Trim();
                    var org = System.Net.WebUtility.HtmlDecode(a.Organization ?? "").Trim();
                    return $"{name} from {org}";
                }));
            
            var dto = new ParsedMetadataDto
            {
                Title = title,
                Abstract = abstractText,
                Authors = authorsString,
                Keywords = string.Join(", ", keywords),
                ResourceUrl = url,
                PublishedDate = DateTime.UtcNow 
            };

            return Result<ParsedMetadataDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<ParsedMetadataDto>.Failure($"Failed to parse ISO XML: {ex.Message}");
        }
    }
}