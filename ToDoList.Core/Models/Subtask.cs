using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace ToDoList.Models
{
    public class Subtask
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int TaskId { get; set; }
        public bool IsCompleted { get; set; } = false;
    }
}
