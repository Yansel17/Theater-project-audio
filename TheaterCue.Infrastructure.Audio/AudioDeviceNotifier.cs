using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace TheaterCue.Infrastructure.Audio;

internal sealed class AudioDeviceNotifier : IMMNotificationClient, IDisposable
{
    private readonly MMDeviceEnumerator _enumerator;
    public event Action? DevicesChanged;

    public AudioDeviceNotifier()
    {
        _enumerator = new MMDeviceEnumerator();
        _enumerator.RegisterEndpointNotificationCallback(this);
    }

    public void OnDeviceAdded(string deviceId) => DevicesChanged?.Invoke();
    public void OnDeviceRemoved(string deviceId) => DevicesChanged?.Invoke();
    public void OnDeviceStateChanged(string deviceId, DeviceState newState) => DevicesChanged?.Invoke();
    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) => DevicesChanged?.Invoke();
    public void OnPropertyValueChanged(string deviceId, PropertyKey key) { }

    public void Dispose()
    {
        _enumerator.UnregisterEndpointNotificationCallback(this);
        _enumerator.Dispose();
    }
}