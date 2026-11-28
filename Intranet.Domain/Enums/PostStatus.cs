namespace Intranet.Domain.Enums;
public enum PostStatus { Draft, PendingApproval, Approved, Published, Archived, Rejected }
public enum PostVisibility { Organisation, Department, Team, Individual }
public enum AudienceType { Organisation, Department, Team, Individual, EmploymentCategory }
public enum ReactionType { Like, Celebrate, Support }
public enum CommentStatus { Active, Hidden, Deleted }
public enum AcknowledgementStatus { Pending, Acknowledged }
public enum OutboxStatus { Pending, Processed, Failed }
