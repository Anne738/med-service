using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace med_service.ViewModels
{
    public class HospitalViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Назва обов'язкова")]
        [MinLength(3, ErrorMessage = "Назва має містити щонайменше 3 символи")]
        [StringLength(100, ErrorMessage = "Назва має бути до 100 символів")]
        [DisplayName("Назва лікарні")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Адреса обов'язкова")]
        [MinLength(5, ErrorMessage = "Адреса має містити щонайменше 5 символів")]
        [StringLength(200, ErrorMessage = "Адреса має бути до 200 символів")]
        [DisplayName("Адреса")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Контакт обов'язковий")]
        [RegularExpression(@"^\+[\d\s\-().]{6,20}$", ErrorMessage = "Невірний міжнародний номер телефону")]
        [DisplayName("Контактний номер")]
        public string Contact { get; set; }

    }
}
