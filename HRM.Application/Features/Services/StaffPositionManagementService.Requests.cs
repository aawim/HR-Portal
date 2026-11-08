using System.Data;
using System.Text.Json;
using HRM.DTOs;
using HRM.Models;
namespace HRM.Services;

public sealed record StaffPositionRequestRow(int Id, int StateId, string Reference, string Staff, string Kind,
    string Position, DateTime From, DateTime? To, string Reason, string Status, string Stage,
    List<string> History, bool CanApprove, bool CanCancel);
