using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using ShareX.Imgur.Plugin;

namespace ShareX.Imgur.Plugin.ViewModels;

/// <summary>
/// ViewModel for Imgur configuration
/// </summary>
public partial class ImgurConfigViewModel : ObservableObject
{
    [ObservableProperty]
    private string _clientId = "30d41ft9z9r8jtt"; // Default ShareX client ID

    [ObservableProperty]
    private int _accountTypeIndex = 0;

    [ObservableProperty]
    private string _albumId = string.Empty;

    [ObservableProperty]
    private int _thumbnailTypeIndex = 4; // Large thumbnail default

    [ObservableProperty]
    private bool _useDirectLink = true;

    [ObservableProperty]
    private string? _statusMessage;

    public void LoadFromJson(string json)
    {
        try
        {
            var config = JsonConvert.DeserializeObject<ImgurConfigModel>(json);
            if (config != null)
            {
                ClientId = config.ClientId ?? "30d41ft9z9r8jtt";
                AccountTypeIndex = (int)config.AccountType;
                AlbumId = config.SelectedAlbum?.id ?? string.Empty;
                ThumbnailTypeIndex = (int)config.ThumbnailType;
                UseDirectLink = config.DirectLink;
            }
        }
        catch
        {
            StatusMessage = "Failed to load configuration";
        }
    }

    public string ToJson()
    {
        var config  = new ImgurConfigModel
        {
            ClientId = ClientId,
            AccountType = (AccountType)AccountTypeIndex,
            ThumbnailType = (ImgurThumbnailType)ThumbnailTypeIndex,
            DirectLink = UseDirectLink,
            UploadToSelectedAlbum = !string.IsNullOrWhiteSpace(AlbumId)
        };

        if (!string.IsNullOrWhiteSpace(AlbumId))
        {
            config.SelectedAlbum = new ImgurAlbumData { id = AlbumId };
        }

        return JsonConvert.SerializeObject(config, Formatting.Indented);
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            StatusMessage = "Client ID is required";
            return false;
        }

        StatusMessage = null;
        return true;
    }
}
