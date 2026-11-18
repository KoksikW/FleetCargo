using System.Windows.Media;
using FleetCargo.Services;

namespace FleetCargo.ViewModels;

/// <summary>Вікно «Авторство»: відомості про автора й проєкт та незмінне фото автора.</summary>
public class AuthorViewModel : ViewModelBase
{
    public AuthorViewModel()
    {
        // Фото автора лежить у каталозі Photos і не змінюється користувачем
        Photo = PhotoService.LoadImage(PhotoService.FindFile(ProjectInfo.AuthorPhotoName));
    }

    public string FullName => ProjectInfo.AuthorName;
    public string GroupText => $"група {ProjectInfo.Group}";
    public string ProjectTitle => ProjectInfo.Title;
    public string Technologies => ProjectInfo.Technologies;
    public string StartDateText => ProjectInfo.StartDate.ToString("dd.MM.yyyy");
    public string EndDateText => ProjectInfo.EndDate?.ToString("dd.MM.yyyy") ?? "—";

    public ImageSource? Photo { get; }
    public bool NoPhoto => Photo == null;
}
