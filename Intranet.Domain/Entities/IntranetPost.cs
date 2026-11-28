using Intranet.Domain.Enums;
namespace Intranet.Domain.Entities;
public class IntranetPost
{
    public long IntranetPostID { get; set; }
    public long OrganisationBusinessEntityID { get; set; }
    public string PostType { get; set; } = "GeneralNews";
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public long? CoverImageID { get; set; }
    public PostStatus Status { get; set; } = PostStatus.Draft;
    public PostVisibility Visibility { get; set; } = PostVisibility.Organisation;
    public bool IsFeatured { get; set; }
    public bool IsPinned { get; set; }
    public bool AllowComments { get; set; } = true;
    public bool AllowReactions { get; set; } = true;
    public bool RequiresAcknowledgement { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? PublishFrom { get; set; }
    public DateTime? PublishUntil { get; set; }
    public long CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public long? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public long? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public byte[]? RowVersion { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<IntranetPostCategoryAssignment> CategoryAssignments { get; set; } = new List<IntranetPostCategoryAssignment>();
    public ICollection<IntranetPostAudience> Audiences { get; set; } = new List<IntranetPostAudience>();
    public ICollection<IntranetPostAttachment> Attachments { get; set; } = new List<IntranetPostAttachment>();
    public ICollection<IntranetPostComment> Comments { get; set; } = new List<IntranetPostComment>();
    public ICollection<IntranetPostReaction> Reactions { get; set; } = new List<IntranetPostReaction>();
    public ICollection<IntranetPostAcknowledgement> Acknowledgements { get; set; } = new List<IntranetPostAcknowledgement>();
    public ICollection<IntranetPostReadState> ReadStates { get; set; } = new List<IntranetPostReadState>();
    public ICollection<IntranetPublicationHistory> PublicationHistory { get; set; } = new List<IntranetPublicationHistory>();
    public IntranetNewJoinerProfile? NewJoinerProfile { get; set; }
}
