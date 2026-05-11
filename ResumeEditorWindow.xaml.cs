using System.Windows;
using System.Windows.Controls;
using CVDesktopEditor.Models;
using CVDesktopEditor.Services;

namespace CVDesktopEditor
{
    public partial class ResumeEditorWindow : Window
    {
        private readonly string _languageCode;
        private readonly ResumeStorageService _storageService;
        private readonly ResumeStore _store;

        public ResumeLanguageData CurrentResume { get; set; }

        public ResumeEditorWindow(string languageCode)
        {
            InitializeComponent();

            _languageCode = languageCode.ToLower();
            _storageService = new ResumeStorageService();
            _store = _storageService.Load();

            CurrentResume = _languageCode == "en" ? _store.English : _store.Spanish;
            EnsureCollections();

            DataContext = CurrentResume;

            if (_languageCode == "en")
            {
                TxtTitle.Text = "English CV Editor";
                TxtSubtitle.Text = "Edit your English CV and save the information locally.";
            }
            else
            {
                TxtTitle.Text = "Editor de CV en Español";
                TxtSubtitle.Text = "Edita tu CV en español y guarda la información localmente.";
            }
        }

        private void EnsureCollections()
        {
            CurrentResume.Experience ??= new System.Collections.ObjectModel.ObservableCollection<ExperienceItem>();
            CurrentResume.Projects ??= new System.Collections.ObjectModel.ObservableCollection<ProjectItem>();
            CurrentResume.Education ??= new System.Collections.ObjectModel.ObservableCollection<EducationItem>();
            CurrentResume.Skills ??= new System.Collections.ObjectModel.ObservableCollection<SkillItem>();

            foreach (var experience in CurrentResume.Experience)
            {
                experience.Responsibilities ??= new System.Collections.ObjectModel.ObservableCollection<TextLineItem>();
            }

            foreach (var project in CurrentResume.Projects)
            {
                project.Details ??= new System.Collections.ObjectModel.ObservableCollection<TextLineItem>();
            }
        }

        private void BtnAddExperience_Click(object sender, RoutedEventArgs e)
        {
            var item = new ExperienceItem();
            item.Responsibilities.Add(new TextLineItem());
            CurrentResume.Experience.Add(item);
        }

        private void BtnRemoveExperience_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ExperienceItem item)
            {
                CurrentResume.Experience.Remove(item);
            }
        }

        private void BtnAddResponsibility_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ExperienceItem item)
            {
                item.Responsibilities ??= new System.Collections.ObjectModel.ObservableCollection<TextLineItem>();
                item.Responsibilities.Add(new TextLineItem());
            }
        }

        private void BtnRemoveResponsibility_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TextLineItem line)
            {
                foreach (var experience in CurrentResume.Experience)
                {
                    if (experience.Responsibilities.Contains(line))
                    {
                        experience.Responsibilities.Remove(line);
                        break;
                    }
                }
            }
        }

        private void BtnAddProject_Click(object sender, RoutedEventArgs e)
        {
            var item = new ProjectItem();
            item.Details.Add(new TextLineItem());
            CurrentResume.Projects.Add(item);
        }

        private void BtnRemoveProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ProjectItem item)
            {
                CurrentResume.Projects.Remove(item);
            }
        }

        private void BtnAddProjectDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ProjectItem item)
            {
                item.Details ??= new System.Collections.ObjectModel.ObservableCollection<TextLineItem>();
                item.Details.Add(new TextLineItem());
            }
        }

        private void BtnRemoveProjectDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TextLineItem line)
            {
                foreach (var project in CurrentResume.Projects)
                {
                    if (project.Details.Contains(line))
                    {
                        project.Details.Remove(line);
                        break;
                    }
                }
            }
        }

        private void BtnAddEducation_Click(object sender, RoutedEventArgs e)
        {
            CurrentResume.Education.Add(new EducationItem());
        }

        private void BtnRemoveEducation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is EducationItem item)
            {
                CurrentResume.Education.Remove(item);
            }
        }

        private void BtnAddSkill_Click(object sender, RoutedEventArgs e)
        {
            CurrentResume.Skills.Add(new SkillItem());
        }

        private void BtnRemoveSkill_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SkillItem item)
            {
                CurrentResume.Skills.Remove(item);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_languageCode == "en")
                _store.English = CurrentResume;
            else
                _store.Spanish = CurrentResume;

            _storageService.Save(_store);
            AppDialogWindow.ShowInfo(this, "Guardar", "Cambios guardados correctamente.");
        }

        private void BtnPreview_Click(object sender, RoutedEventArgs e)
        {
            var preview = new ResumeWebPreviewWindow(CurrentResume, _languageCode == "en");
            preview.ShowDialog();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
