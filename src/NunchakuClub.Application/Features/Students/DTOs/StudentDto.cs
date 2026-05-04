using System;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.Students.DTOs;

public class StudentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public Guid? CurrentBeltRankId { get; set; }
    public string? BeltRankName { get; set; }
    public string? Address { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public Gender Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime JoinDate { get; set; }
    public StudentLearningStatus LearningStatus { get; set; }
    public StudentClassRole ClassRole { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }
    public string? Notes { get; set; }
}

public class CreateStudentDto
{
    public Guid UserId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public Guid? CurrentBeltRankId { get; set; }
    public string? Address { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public Gender Gender { get; set; } = Gender.Male;
    public DateTime? DateOfBirth { get; set; }
    public StudentLearningStatus LearningStatus { get; set; } = StudentLearningStatus.Active;
    public StudentClassRole ClassRole { get; set; } = StudentClassRole.Member;
    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }
    public string? Notes { get; set; }
}

public class UpdateStudentDto
{
    public string StudentCode { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public Guid? CurrentBeltRankId { get; set; }
    public string? Address { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public Gender Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public StudentLearningStatus LearningStatus { get; set; }
    public StudentClassRole ClassRole { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }
    public string? Notes { get; set; }
}
