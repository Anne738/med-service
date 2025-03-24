﻿using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using static med_service.Models.User; //Importing UserRole enum

namespace med_service.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; } //ID for editing and deleting

        [Required (ErrorMessage = "The First Name field is required")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "First Name can only contain letters and spaces")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required (ErrorMessage = "The Last Name field is required")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Last Name can only contain letters and spaces")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required (ErrorMessage = "The Email field is required")]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required (ErrorMessage = "The Username field is required")]
        [MinLength(5, ErrorMessage = "Username must be at least 5 characters long")]
        [MaxLength(15, ErrorMessage = "Username cannot be longer than 15 characters")]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Required (ErrorMessage = "The Role field is required")]
        [Display(Name = "Role")]
        public UserRole Role { get; set; } //Enum from the User model
    }
}
