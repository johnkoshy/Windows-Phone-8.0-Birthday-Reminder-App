using System;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Phone.Scheduler;
using System.Diagnostics;

namespace Windows_Phone_8._0_Birthday_Reminder_App
{
    public partial class MainPage : PhoneApplicationPage
    {
        private ObservableCollection<Birthday> _birthdays = new ObservableCollection<Birthday>();
        private const string BIRTHDAYS_KEY = "Birthdays";

        public MainPage()
        {
            InitializeComponent();
            DataContext = this;
            BirthdayList.ItemsSource = _birthdays;
            LoadBirthdays();
        }

        public ObservableCollection<Birthday> Birthdays
        {
            get { return _birthdays; }
        }

        private void LoadBirthdays()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(BIRTHDAYS_KEY))
            {
                var savedBirthdays = (List<Birthday>)settings[BIRTHDAYS_KEY];
                foreach (var birthday in savedBirthdays)
                {
                    _birthdays.Add(birthday);
                    ScheduleReminder(birthday);
                }
            }
        }

        private void DebugListAllReminders_Click(object sender, RoutedEventArgs e)
        {
            var allReminders = ScheduledActionService.GetActions<Reminder>();
            var output = new System.Text.StringBuilder();
            
            output.AppendLine(string.Format("Found {0} reminders in system:", allReminders.Count()));
            foreach (var r in allReminders)
            {
                output.AppendLine(string.Format("- {0} (Title: {1}, Time: {2})", 
                    r.Name, r.Title, r.BeginTime));
            }
            
            MessageBox.Show(output.ToString());
        }

        private void AddBirthdayButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/AddBirthdayPage.xaml", UriKind.Relative));
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (NavigationContext.QueryString.ContainsKey("Name") && NavigationContext.QueryString.ContainsKey("Date"))
            {
                string name = NavigationContext.QueryString["Name"];
                DateTime date;
                if (DateTime.TryParse(NavigationContext.QueryString["Date"], out date))
                {
                    var birthday = new Birthday { Name = name, Date = date };
                    _birthdays.Add(birthday);
                    SaveBirthdays();
                    ScheduleReminder(birthday);
                    Debug.WriteLine(String.Format("Added birthday for {0} on {1}", name, date));
                }
                else
                {
                    MessageBox.Show("Error: Invalid date format.");
                }
            }
        }

        private void SaveBirthdays()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            settings[BIRTHDAYS_KEY] = _birthdays.ToList();
            settings.Save();
        }

        private void CheckRemindersButton_Click(object sender, RoutedEventArgs e)
        {
            var reminders = ScheduledActionService.GetActions<Reminder>();
            
            if (!reminders.Any())
            {
                MessageBox.Show("No reminders found in system.");
                return;
            }

            var output = new System.Text.StringBuilder();
            output.AppendLine("SYSTEM REMINDERS:");
            foreach (var r in reminders)
            {
                output.AppendLine(string.Format("\nNAME: {0}", r.Name));
                output.AppendLine(string.Format("TITLE: {0}", r.Title));
                output.AppendLine(string.Format("CONTENT: {0}", r.Content));
                output.AppendLine(string.Format("BEGIN TIME: {0}", r.BeginTime));
                output.AppendLine(string.Format("EXPIRATION TIME: {0}", r.ExpirationTime));
                output.AppendLine(string.Format("RECURRENCE: {0}", r.RecurrenceType));
            }
            
            MessageBox.Show(output.ToString());
        }

        private void ScheduleReminder(Birthday birthday)
        {
            string reminderName = "BirthdayReminder_" + Uri.EscapeDataString(birthday.Name);

            // Debug output
            Debug.WriteLine(String.Format("Creating reminder for {0} on {1}", birthday.Name, birthday.Date));

            // Remove existing if any
            if (ScheduledActionService.Find(reminderName) != null)
            {
                ScheduledActionService.Remove(reminderName);
                Debug.WriteLine(String.Format("Removed existing reminder: {0}", reminderName));
            }

            // Calculate times (8AM on birthday)
            DateTime beginTime = new DateTime(DateTime.Now.Year, birthday.Date.Month, birthday.Date.Day, 8, 0, 0);
            // Set expiration far in the future for yearly recurrence
            DateTime expireTime = DateTime.Now.AddYears(10); // 10 years from now

            // Adjust for past dates
            if (beginTime < DateTime.Now)
            {
                beginTime = beginTime.AddYears(1);
            }

            try
            {
                // Create reminder
                var reminder = new Reminder(reminderName)
                {
                    Title = "Birthday Reminder",
                    Content = String.Format("{0}'s birthday!", birthday.Name),
                    BeginTime = beginTime,
                    ExpirationTime = expireTime,
                    RecurrenceType = RecurrenceInterval.Yearly,
                    NavigationUri = new Uri("/MainPage.xaml", UriKind.Relative)
                };

                // Add to system
                ScheduledActionService.Add(reminder);

                // Immediate verification
                var exists = ScheduledActionService.Find(reminderName) != null;
                Debug.WriteLine(String.Format("Reminder '{0}' created: {1}, BeginTime: {2}, ExpirationTime: {3}", reminderName, exists, beginTime, expireTime));
                MessageBox.Show(String.Format("Reminder '{0}' created successfully: {1}\nWill trigger at: {2}\nExpires at: {3}",
                    reminderName, exists, beginTime, expireTime));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("Error creating reminder '{0}': {1}", reminderName, ex.Message));
                MessageBox.Show(String.Format("Error creating reminder: {0}", ex.Message));
            }
        }

        private void CreateTestReminder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string testName = "TEST_" + DateTime.Now.Ticks.ToString();
                var reminder = new Reminder(testName)
                {
                    Title = "TEST REMINDER",
                    Content = "This is a test notification",
                    BeginTime = DateTime.Now.AddMinutes(1),
                    ExpirationTime = DateTime.Now.AddMinutes(2),
                    RecurrenceType = RecurrenceInterval.None
                };

                ScheduledActionService.Add(reminder);
                MessageBox.Show(string.Format("Created test reminder '{0}'", testName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error: {0}", ex.ToString()));
            }
        }

        private void AddTestReminder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string reminderName = "TEST_REMINDER_123";

                // Remove any existing reminder first
                if (ScheduledActionService.Find(reminderName) != null)
                {
                    ScheduledActionService.Remove(reminderName);
                }

                var testReminder = new Reminder(reminderName)
                {
                    Title = "TEST REMINDER",
                    Content = "This is a test notification",
                    BeginTime = DateTime.Now.AddMinutes(1),
                    ExpirationTime = DateTime.Now.AddMinutes(2),
                    RecurrenceType = RecurrenceInterval.None,
                    NavigationUri = new Uri("/MainPage.xaml", UriKind.Relative)
                };

                ScheduledActionService.Add(testReminder);
                MessageBox.Show("Test reminder added for 1 minute from now");
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("General error: " + ex.Message);
            }
        }
    }

    public class Birthday
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
    }
}