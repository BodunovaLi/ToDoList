using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace ToDoList.Models
{
    public class TaskItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int CategoryId { get; set; }

        public bool IsCompleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }
        [Ignore]
        public Category? Category { get; set; }

    }
}
