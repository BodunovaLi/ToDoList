using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace ToDoList.Models
{
    public class Subtask : System.ComponentModel.INotifyPropertyChanged
    {
        private string _name = "";
        private bool _isCompleted;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                PropertyChanged?.Invoke(this,
                    new System.ComponentModel.PropertyChangedEventArgs(nameof(Name)));
            }
        }
        public int TaskId { get; set; }
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted == value) return;
                _isCompleted = value;
                PropertyChanged?.Invoke(this,
                    new System.ComponentModel.PropertyChangedEventArgs(nameof(IsCompleted)));
            }
        }
    }
}
