using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace ToDoList.Models
{
    public class TaskItem : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isCompleted;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int CategoryId { get; set; }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted == value)
                    return;

                _isCompleted = value;
                PropertyChanged?.Invoke(this,
                    new System.ComponentModel.PropertyChangedEventArgs(nameof(IsCompleted)));
            }
        }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }
        [Ignore]
        public Category? Category { get; set; }

    }
}
