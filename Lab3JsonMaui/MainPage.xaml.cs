using System;
using System.Collections.Generic;
using System.Linq;
using Lab3JsonMaui.Models;
using Lab3JsonMaui.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

namespace Lab3JsonMaui
{
    public partial class MainPage : ContentPage
    {
        private readonly JsonStorageService _jsonService = new();

        // повний список усіх подій
        private readonly List<ParliamentEvent> _allEvents = new();

        private string? _currentFilePath;

        // поточний вибраний елемент + його візуальний рядок
        private ParliamentEvent? _selectedEvent;
        private View? _selectedRowView;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

		// робота з файлом JSON
        private async void OnOpenJsonClicked(object sender, EventArgs e)
		{
			var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
			{
				{ DevicePlatform.WinUI,      new[] { ".json" } },
				{ DevicePlatform.Android,    new[] { ".json" } },
				{ DevicePlatform.iOS,        new[] { ".json" } },
				{ DevicePlatform.MacCatalyst,new[] { ".json" } },
			});

			var options = new PickOptions
			{
				PickerTitle = "Виберіть JSON-файл з подіями",
				FileTypes = customFileType
			};

			var result = await FilePicker.Default.PickAsync(options);
			if (result == null)
				return;

			_currentFilePath = result.FullPath;

			var items = await _jsonService.LoadFromFileAsync(_currentFilePath);
			_allEvents.Clear();
			_allEvents.AddRange(items);

			ApplyFilter();

			await DisplayAlert("Файл відкрито",
				$"Зчитано записів: {_allEvents.Count}", "OK");
		}

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

        private async void OnAboutClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(AboutPage));
        }

        // фільтрація (LINQ)
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

            RefreshEventsView(query.ToList());
        }

        // візуалізація списку в EventsStack 
        private void RefreshEventsView(List<ParliamentEvent> items)
        {
            EventsStack.Children.Clear();
            _selectedEvent = null;
            _selectedRowView = null;

            foreach (var ev in items)
            {
                var row = CreateRowForEvent(ev);
                EventsStack.Children.Add(row);
            }
        }

        private View CreateRowForEvent(ParliamentEvent ev)
        {
            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }
                },
                Padding = 4,
                Margin = new Thickness(0, 1),
                BackgroundColor = Color.FromArgb("#202020")
            };

            grid.Add(new Label { Text = ev.FullName, TextColor = Colors.White, FontSize = 14 }, 0, 0);
            grid.Add(new Label { Text = ev.Faculty, TextColor = Colors.White, FontSize = 14 }, 1, 0);
            grid.Add(new Label { Text = ev.Department, TextColor = Colors.White, FontSize = 14 }, 2, 0);
            grid.Add(new Label { Text = ev.Speciality, TextColor = Colors.White, FontSize = 14 }, 3, 0);
            grid.Add(new Label { Text = ev.EventType, TextColor = Colors.White, FontSize = 14 }, 4, 0);
            grid.Add(new Label { Text = ev.TimeFrame, TextColor = Colors.White, FontSize = 14 }, 5, 0);

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => OnEventRowTapped(ev, grid);
            grid.GestureRecognizers.Add(tap);

            return grid;
        }

        private void OnEventRowTapped(ParliamentEvent ev, Grid rowView)
        {
            // зняти виділення з попереднього рядка
            if (_selectedRowView is Grid oldGrid)
                oldGrid.BackgroundColor = Color.FromArgb("#202020");

            _selectedEvent = ev;
            _selectedRowView = rowView;
            rowView.BackgroundColor = Color.FromArgb("#404040");

            FillFormFromEvent(ev);
        }

        private void FillFormFromEvent(ParliamentEvent ev)
        {
            FullNameEntry.Text = ev.FullName;
            FacultyEntry.Text = ev.Faculty;
            DepartmentEntry.Text = ev.Department;
            SpecialityEntry.Text = ev.Speciality;
            EventTypeEntry.Text = ev.EventType;
            TimeFrameEntry.Text = ev.TimeFrame;
            DescriptionEntry.Text = ev.Description;
        }

        // форма: очищення / додавання / редагування / видалення

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

            _selectedEvent = null;

            if (_selectedRowView is Grid oldGrid)
                oldGrid.BackgroundColor = Color.FromArgb("#202020");

            _selectedRowView = null;
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
            if (_selectedEvent == null)
            {
                await DisplayAlert("Увага", "Оберіть запис у таблиці для редагування.", "OK");
                return;
            }

            var updated = BuildEventFromForm();
            if (updated == null)
                return;

            _selectedEvent.FullName = updated.FullName;
            _selectedEvent.Faculty = updated.Faculty;
            _selectedEvent.Department = updated.Department;
            _selectedEvent.Speciality = updated.Speciality;
            _selectedEvent.EventType = updated.EventType;
            _selectedEvent.TimeFrame = updated.TimeFrame;
            _selectedEvent.Description = updated.Description;

            var index = _allEvents.FindIndex(ev => ev.Id == _selectedEvent.Id);
            if (index >= 0)
                _allEvents[index] = _selectedEvent;

            ApplyFilter();
            await DisplayAlert("Змінено", "Дані події оновлено.", "OK");
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            if (_selectedEvent == null)
            {
                await DisplayAlert("Увага", "Оберіть запис у таблиці для видалення.", "OK");
                return;
            }

            var confirm = await DisplayAlert("Підтвердження",
                "Видалити вибрану подію?", "Так", "Ні");

            if (!confirm)
                return;

            _allEvents.RemoveAll(ev => ev.Id == _selectedEvent.Id);
            ApplyFilter();
            ClearForm();
        }

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
    }
}