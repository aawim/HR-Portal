using HRM.DTOs;
namespace HRM.Application.Contracts.Pages;

public sealed record PayFrameworkInput(int Id,string Code,string Name,bool Active);
public sealed record MatrixStepInput(int Number,decimal JobAllowance,decimal? BasicSalaryOverride,bool Active=true);
public sealed record MatrixBenefitInput(string Name,decimal? Amount,string Frequency="Monthly");
public sealed record JobMatrixInput(int Id,int FrameworkId,string Code,string Name,int CurrencyId,decimal BasicSalary,
    DateTime From,DateTime? To,bool Active,List<string> Titles,List<MatrixStepInput> Steps,List<MatrixBenefitInput> Benefits);
public sealed record MatrixStepOption(int Id,int Number,decimal BasicSalary,decimal JobAllowance,bool Active);
public sealed record JobMatrixRow(int Id,int FrameworkId,string Code,string Name,int CurrencyId,decimal BasicSalary,
    DateTime From,DateTime? To,bool Active,bool InUse,List<PositionLookup> Titles,List<MatrixStepOption> Steps,List<MatrixBenefitInput> Benefits);
public sealed record PositionMatrixInput(int JobMatrixId,int TitleId,int StepId,int Slots,DateTime From,DateTime? To);
public sealed record PositionPayInfo(string Currency,decimal BasicSalary,decimal JobAllowance,List<MatrixBenefitInput> Benefits);
public sealed record PayFrameworkData(List<PositionFrameworkOption> Frameworks,List<JobMatrixRow> Matrices,
    List<PositionLookup> Currencies,bool CanManageFrameworks,bool CanManagePositions);
