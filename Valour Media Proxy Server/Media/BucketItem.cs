using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Valour.MPS.Media;

public enum ContentCategory
{
    Planet,
    Profile,
    Image,
    File,
    App
}

[Table("bucket_items")]
public class BucketItem
{
    [Key]
    [Column("id")]
    public string Id { get; set; }

    [Column("hash")]
    public string Hash { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("mime_type")]
    public string MimeType { get; set; }

    [Column("category")]
    public ContentCategory Category { get; set; }

    /// <summary>
    /// The url for the bucket item
    /// </summary>
    [JsonIgnore]
    public string Url => $"https://vmps.valour.gg/content/{Category}/{UserId}/{Hash}";
}
