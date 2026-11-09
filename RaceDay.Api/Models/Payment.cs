using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    /// <summary>
    /// Records payment for an Enrolment. Carried over from the Part 1 schema for
    /// continuity. No endpoints were planned for Payments in the Part 1 API plan,
    /// so no PaymentsController is implemented in Part 2; the table exists purely
    /// to keep the database schema consistent with the approved Part 1 ERD/SQL script.
    /// </summary>
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int EnrolmentId { get; set; }

        [ForeignKey(nameof(EnrolmentId))]
        public Enrolment? Enrolment { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(30)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string PaymentStatus { get; set; } = "Pending";
    }
}
