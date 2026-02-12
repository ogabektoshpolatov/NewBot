namespace bot.Models;

public class CB
{
    public static string Task(int taskId) => $"task:{taskId}";
    public static string AddUser(int taskId) => $"task:{taskId}:addUser";
    public static string AddUserConfirm(int taskId) => $"task:{taskId}:addUser:confirm";
    public static string RemoveUser(int taskId) => $"task:{taskId}:removeUser";
    public static string ViewUsers(int taskId) => $"task:{taskId}:viewUsers";
}