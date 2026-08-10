using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class HocSinhPhuHuynh
    {
        [Key]
        public int IdHocSinhPhuHuynh { get; set; }

        public int IdHocSinh { get; set; }

        [ForeignKey(nameof(IdHocSinh))]
        public HocSinh HocSinh { get; set; }

        public int IdPhuHuynh { get; set; }

        [ForeignKey(nameof(IdPhuHuynh))]
        public PhuHuynh PhuHuynh { get; set; }

        [StringLength(50)]
        public string? QuanHe { get; set; } 

        public bool LaLienHeChinh { get; set; } = false;
    }
}