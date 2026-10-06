using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.WinUI.Core.Services;

namespace POS.WinUI.ViewModels.CustomerFacing;

public sealed partial class CfdStandbyViewModel : ObservableObject, IDisposable
{
    private readonly SessionService _sessionService;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _portraitTimer;
    private readonly DispatcherTimer _landscapeTimer;
    private readonly DispatcherTimer _subTimer;
    private bool _disposed;

    private int _portraitIndex;
    private int _landscapeIndex;
    private int _subPairIndex;

    [ObservableProperty] private string _storeBranchName = "FLAGSHIP STORE - QUẬN 1";
    [ObservableProperty] private string _currentTime = DateTime.Now.ToString("HH:mm:ss");
    [ObservableProperty] private string _welcomeStatus = "Đang phục vụ";
    [ObservableProperty] private string _wifiInfo = "OraPOS_Guest";
    [ObservableProperty] private string _hotlineInfo = "1900 8888";

    // ── 1. Top-Left: Portrait Banner (35%) ──
    [ObservableProperty] private string _portraitImagePath = CfdStandbyBannerData.PortraitBanners[0].ImagePath;
    [ObservableProperty] private string _portraitTag = CfdStandbyBannerData.PortraitBanners[0].Tag;
    [ObservableProperty] private string _portraitTitle = CfdStandbyBannerData.PortraitBanners[0].Title;
    [ObservableProperty] private string _portraitPriceBadge = CfdStandbyBannerData.PortraitBanners[0].PriceBadge;

    // ── 2. Top-Right: Landscape Banner (65%) ──
    [ObservableProperty] private string _landscapeImagePath = CfdStandbyBannerData.LandscapeBanners[0].ImagePath;
    [ObservableProperty] private string _landscapeTag = CfdStandbyBannerData.LandscapeBanners[0].Tag;
    [ObservableProperty] private string _landscapeTitle = CfdStandbyBannerData.LandscapeBanners[0].Title;
    [ObservableProperty] private string _landscapePriceBadge = CfdStandbyBannerData.LandscapeBanners[0].PriceBadge;

    // ── 3 & 4. Bottom Row: Dual Sub-Banners (50% / 50%) ──
    [ObservableProperty] private string _subBanner1ImagePath = CfdStandbyBannerData.SubPairs[0].Card1.ImagePath;
    [ObservableProperty] private string _subBanner1Tag = CfdStandbyBannerData.SubPairs[0].Card1.Tag;
    [ObservableProperty] private string _subBanner1Title = CfdStandbyBannerData.SubPairs[0].Card1.Title;
    [ObservableProperty] private string _subBanner1Badge = CfdStandbyBannerData.SubPairs[0].Card1.PriceBadge;

    [ObservableProperty] private string _subBanner2ImagePath = CfdStandbyBannerData.SubPairs[0].Card2.ImagePath;
    [ObservableProperty] private string _subBanner2Tag = CfdStandbyBannerData.SubPairs[0].Card2.Tag;
    [ObservableProperty] private string _subBanner2Title = CfdStandbyBannerData.SubPairs[0].Card2.Title;
    [ObservableProperty] private string _subBanner2Badge = CfdStandbyBannerData.SubPairs[0].Card2.PriceBadge;

    public CfdStandbyViewModel(SessionService sessionService)
    {
        _sessionService = sessionService;

        if (!string.IsNullOrWhiteSpace(_sessionService.StoreName))
        {
            StoreBranchName = _sessionService.StoreName.ToUpperInvariant();
        }

        _clockTimer = CreateTimer(TimeSpan.FromSeconds(1), () => CurrentTime = DateTime.Now.ToString("HH:mm:ss"));
        _portraitTimer = CreateTimer(TimeSpan.FromSeconds(5.5), NextPortraitSlide);
        _landscapeTimer = CreateTimer(TimeSpan.FromSeconds(5.0), NextLandscapeSlide);
        _subTimer = CreateTimer(TimeSpan.FromSeconds(7.0), NextSubPair);
    }

    private static DispatcherTimer CreateTimer(TimeSpan interval, Action onTick)
    {
        var timer = new DispatcherTimer { Interval = interval };
        timer.Tick += (s, e) => onTick();
        timer.Start();
        return timer;
    }

    [RelayCommand]
    public void NextPortraitSlide()
    {
        _portraitIndex = (_portraitIndex + 1) % CfdStandbyBannerData.PortraitBanners.Length;
        var item = CfdStandbyBannerData.PortraitBanners[_portraitIndex];
        (PortraitImagePath, PortraitTag, PortraitTitle, PortraitPriceBadge) = (item.ImagePath, item.Tag, item.Title, item.PriceBadge);
    }

    [RelayCommand]
    public void NextLandscapeSlide()
    {
        _landscapeIndex = (_landscapeIndex + 1) % CfdStandbyBannerData.LandscapeBanners.Length;
        var item = CfdStandbyBannerData.LandscapeBanners[_landscapeIndex];
        (LandscapeImagePath, LandscapeTag, LandscapeTitle, LandscapePriceBadge) = (item.ImagePath, item.Tag, item.Title, item.PriceBadge);
    }

    [RelayCommand]
    public void NextSubPair()
    {
        _subPairIndex = (_subPairIndex + 1) % CfdStandbyBannerData.SubPairs.Length;
        var pair = CfdStandbyBannerData.SubPairs[_subPairIndex];
        (SubBanner1ImagePath, SubBanner1Tag, SubBanner1Title, SubBanner1Badge) = (pair.Card1.ImagePath, pair.Card1.Tag, pair.Card1.Title, pair.Card1.PriceBadge);
        (SubBanner2ImagePath, SubBanner2Tag, SubBanner2Title, SubBanner2Badge) = (pair.Card2.ImagePath, pair.Card2.Tag, pair.Card2.Title, pair.Card2.PriceBadge);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _clockTimer.Stop();
            _portraitTimer.Stop();
            _landscapeTimer.Stop();
            _subTimer.Stop();
            _disposed = true;
        }
    }
}
