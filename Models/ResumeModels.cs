using System.Collections.ObjectModel;

namespace CVDesktopEditor.Models
{
    public class ResumeStore
    {
        public ResumeLanguageData Spanish { get; set; } = new();
        public ResumeLanguageData English { get; set; } = new();

        public string SpanishPdfPath { get; set; } = "";
        public string EnglishPdfPath { get; set; } = "";
    }

    public class ResumeLanguageData
    {
        public string FullName { get; set; } = "";
        public string Location { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Portfolio { get; set; } = "";
        public string LinkedIn { get; set; } = "";
        public string ProfessionalSummary { get; set; } = "";

        public ObservableCollection<ExperienceItem> Experience { get; set; } = new();
        public ObservableCollection<ProjectItem> Projects { get; set; } = new();
        public ObservableCollection<EducationItem> Education { get; set; } = new();
        public ObservableCollection<SkillItem> Skills { get; set; } = new();
    }

    public class ExperienceItem
    {
        public string Company { get; set; } = "";
        public string Position { get; set; } = "";
        public string Location { get; set; } = "";
        public string Period { get; set; } = "";
        public ObservableCollection<TextLineItem> Responsibilities { get; set; } = new();
    }

    public class ProjectItem
    {
        public string Title { get; set; } = "";
        public string Role { get; set; } = "";
        public string Location { get; set; } = "";
        public ObservableCollection<TextLineItem> Details { get; set; } = new();
    }

    public class EducationItem
    {
        public string Institution { get; set; } = "";
        public string Degree { get; set; } = "";
        public string Description { get; set; } = "";
        public string Location { get; set; } = "";
        public string Period { get; set; } = "";
    }

    public class SkillItem
    {
        public string Value { get; set; } = "";
    }

    public class TextLineItem
    {
        public string Value { get; set; } = "";
    }
}