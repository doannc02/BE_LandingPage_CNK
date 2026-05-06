using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

public class CreateStudentDto : IValidatableObject
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string StudentCode { get; set; } = string.Empty;

    [Required]
    public Guid BranchId { get; set; }
    public Guid? CurrentBeltRankId { get; set; }
    public string? Address { get; set; }

    [Range(typeof(decimal), "0", "300")]
    public decimal? HeightCm { get; set; }

    [Range(typeof(decimal), "0", "500")]
    public decimal? WeightKg { get; set; }

    public Gender Gender { get; set; } = Gender.Male;
    public DateTime? DateOfBirth { get; set; }
    public StudentLearningStatus LearningStatus { get; set; } = StudentLearningStatus.Active;
    public StudentClassRole ClassRole { get; set; } = StudentClassRole.Member;

    [MaxLength(255)]
    public string? GuardianName { get; set; }

    [Phone]
    [MaxLength(50)]
    public string? GuardianPhone { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(StudentCode))
        {
            yield return new ValidationResult("Student code is required.", new[] { nameof(StudentCode) });
        }

        if (DateOfBirth.HasValue && DateOfBirth.Value.Date > DateTime.UtcNow.Date)
        {
            yield return new ValidationResult("Date of birth cannot be in the future.", new[] { nameof(DateOfBirth) });
        }
    }
}

public class UpdateStudentDto : IValidatableObject
{
    [Required]
    [MaxLength(50)]
    public string StudentCode { get; set; } = string.Empty;

    [Required]
    public Guid BranchId { get; set; }
    public Guid? CurrentBeltRankId { get; set; }
    public string? Address { get; set; }

    [Range(typeof(decimal), "0", "300")]
    public decimal? HeightCm { get; set; }

    [Range(typeof(decimal), "0", "500")]
    public decimal? WeightKg { get; set; }

    public Gender Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public StudentLearningStatus LearningStatus { get; set; }
    public StudentClassRole ClassRole { get; set; }

    [MaxLength(255)]
    public string? GuardianName { get; set; }

    [Phone]
    [MaxLength(50)]
    public string? GuardianPhone { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(StudentCode))
        {
            yield return new ValidationResult("Student code is required.", new[] { nameof(StudentCode) });
        }

        if (DateOfBirth.HasValue && DateOfBirth.Value.Date > DateTime.UtcNow.Date)
        {
            yield return new ValidationResult("Date of birth cannot be in the future.", new[] { nameof(DateOfBirth) });
        }
    }
}
