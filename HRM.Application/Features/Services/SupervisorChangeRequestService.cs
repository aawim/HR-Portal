using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
namespace HRM.Services;

public sealed record SupervisorRequestRow(int Id,int StateId,string Reference,string Person,string Target,string Change,string Reason,string Status,string Stage,bool CanApprove,bool CanCancel);
