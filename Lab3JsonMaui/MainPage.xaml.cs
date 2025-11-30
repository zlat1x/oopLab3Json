using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Lab3JsonMaui.Models;
using Lab3JsonMaui.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Lab3JsonMaui
{
    public partial class MainPage : ContentPage
    {
        // колекція для прив'язки до CollectionView
        public ObservableCollection<ParliamentEvent> Events { get; } = new();

        private readonly JsonStorageService _jsonService = new();
        private readonly List<ParliamentEvent> _allEvents = new(); 
        private string? _currentFilePath; 

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        // відкриття JSON через диспетчер файлів
        private async void OnOpenJsonClicked(object sender, EventArgs e)
        {
            var options = new PickOptions
            {
                PickerTitle = "Виберіть JSON-файл з подіями",
                FileTypes = FilePickerFileType.Json
            };

            var result = await FilePicker.Default.PickAsync(options);

            if (result == null)
                return;

            _currentFilePath = result.FullPath;

            var items = await _jsonService.LoadFromFileAsync(_currentFilePath);
            _allEvents.Clear();
            _allEvents.AddRange(items);

            ApplyFilter(); // оновити видимий список

            await DisplayAlert("Файл відкрито",
                $"Зчитано записів: {_allEvents.Count}", "OK");
        }

        // збереження до файлу (той же шлях)
        private async void OnSaveJsonClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentFilePath))
            {
                await DisplayAlert("Помилка",
                    "Спочатку відкрийте JSON-файл через кнопку \"Відкрити JSON\".",
                    "OK");
                return;
            }

            await _jsonService.SaveToFileAsync(_currentFilePath, _allEvents);
            await DisplayAlert("Збереження",
                "Дані успішно збережені до файлу.",
                "OK");
        }

        // перехід на сторінку 'про програму'
        private async void OnAboutClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(AboutPage));
        }

        // LINQ-фільтрація
        private void OnApplyFilterClicked(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void OnClearFilterClicked(object sender, EventArgs e)
        {
            FacultyFilterEntry.Text = string.Empty;
            SpecialityFilterEntry.Text = string.Empty;
            EventTypeFilterEntry.Text = string.Empty;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            IEnumerable<ParliamentEvent> query = _allEvents;

            var faculty = FacultyFilterEntry.Text;
            var speciality = SpecialityFilterEntry.Text;
            var eventType = EventTypeFilterEntry.Text;

            if (!string.IsNullOrWhiteSpace(faculty))
            {
                query = query.Where(ev =>
                    ev.Faculty.Contains(faculty, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(speciality))
            {
                query = query.Where(ev =>
                    ev.Speciality.Contains(speciality, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                query = query.Where(ev =>
                    ev.EventType.Contains(eventType, StringComparison.OrdinalIgnoreCase));
            }

            Events.Clear();
            foreach (var item in query)
            {
                Events.Add(item);
            }
        }

        // робота з формою (додавання / редагування / видалення)

        private ParliamentEvent? GetSelectedEvent()
        {
            return EventsCollection.SelectedItem as ParliamentEvent;
        }

        private void OnClearFormClicked(object sender, EventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            FullNameEntry.Text = string.Empty;
            FacultyEntry.Text = string.Empty;
            DepartmentEntry.Text = string.Empty;
            SpecialityEntry.Text = string.Empty;
            EventTypeEntry.Text = string.Empty;
            TimeFrameEntry.Text = string.Empty;
            DescriptionEntry.Text = string.Empty;

            EventsCollection.SelectedItem = null;
        }

        private async void OnAddClicked(object sender, EventArgs e)
        {
            var newEvent = BuildEventFromForm();
            if (newEvent == null)
                return;

            _allEvents.Add(newEvent);
            ApplyFilter();

            await DisplayAlert("Додано", "Нову подію додано до списку.", "OK");
            ClearForm();
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            var selected = GetSelectedEvent();
            if (selected == null)
            {
                await DisplayAlert("Увага", "Оберіть запис у таблиці для редагування.", "OK");
                return;
            }

            var updated = BuildEventFromForm();
            if (updated == null)
                return;

            // перезапис полів вибраного об'єкта
            selected.FullName = updated.FullName;
            selected.Faculty = updated.Faculty;
            selected.Department = updated.Department;
            selected.Speciality = updated.Speciality;
            selected.EventType = updated.EventType;
            selected.TimeFrame = updated.TimeFrame;
            selected.Description = updated.Description;

            // синхронізація _allEvents (по Id)
            var index = _allEvents.FindIndex(ev => ev.Id == selected.Id);
            if (index >= 0)
            {
                _allEvents[index] = selected;
            }

            ApplyFilter();

            await DisplayAlert("Змінено", "Дані події оновлено.", "OK");
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            var selected = GetSelectedEvent();
            if (selected == null)
            {
                await DisplayAlert("Увага", "Оберіть запис у таблиці для видалення.", "OK");
                return;
            }

            var confirm = await DisplayAlert("Підтвердження",
                "Видалити вибрану подію?", "Так", "Ні");

            if (!confirm)
                return;

            _allEvents.RemoveAll(ev => ev.Id == selected.Id);
            ApplyFilter();
            ClearForm();
        }

        // створення об'єкта з полів форми, базова валідація
        private ParliamentEvent? BuildEventFromForm()
        {
            string fullName = FullNameEntry.Text?.Trim() ?? string.Empty;
            string faculty = FacultyEntry.Text?.Trim() ?? string.Empty;
            string department = DepartmentEntry.Text?.Trim() ?? string.Empty;
            string speciality = SpecialityEntry.Text?.Trim() ?? string.Empty;
            string eventType = EventTypeEntry.Text?.Trim() ?? string.Empty;
            string timeFrame = TimeFrameEntry.Text?.Trim() ?? string.Empty;
            string description = DescriptionEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(faculty) ||
                string.IsNullOrWhiteSpace(eventType))
            {
                DisplayAlert("Помилка",
                    "Поля \"П.І.П.\", \"Факультет\" та \"Тип заходу\" є обов'язковими.",
                    "OK");
                return null;
            }

            return new ParliamentEvent
            {
                FullName = fullName,
                Faculty = faculty,
                Department = department,
                Speciality = speciality,
                EventType = eventType,
                TimeFrame = timeFrame,
                Description = description
            };
        }

        // користувач клацає по елементу списку – заповнення форми
        protected override void OnAppearing()
        {
            base.OnAppearing();
            EventsCollection.SelectionChanged += EventsCollection_SelectionChanged;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            EventsCollection.SelectionChanged -= EventsCollection_SelectionChanged;
        }

        private void EventsCollection_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var selected = GetSelectedEvent();
            if (selected == null)
                return;

            FullNameEntry.Text = selected.FullName;
            FacultyEntry.Text = selected.Faculty;
            DepartmentEntry.Text = selected.Department;
            SpecialityEntry.Text = selected.Speciality;
            EventTypeEntry.Text = selected.EventType;
            TimeFrameEntry.Text = selected.TimeFrame;
            DescriptionEntry.Text = selected.Description;
        }
    }
}