using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Video;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class PlaceTrackedImages : MonoBehaviour
{
    // Reference to AR tracked image manager component
    private ARTrackedImageManager _trackedImagesManager;

    public ToastMessage toastMessage;

    // List of prefabs to instantiate - these should be named the same
    // as their corresponding 2D images in the reference image library 
    public GameObject[] ArPrefabs;

    private readonly Dictionary<string, GameObject> _instantiatedPrefabs = new Dictionary<string, GameObject>();

    void Awake()
    {
        _trackedImagesManager = GetComponent<ARTrackedImageManager>();
    }
    private void OnEnable()
    {
        _trackedImagesManager.trackedImagesChanged += OnTrackedImagesChanged;
    }
    private void OnDisable()
    {
        _trackedImagesManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }
    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // Loop through all new tracked images that have been detected
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            // Get the name of the reference image
            String imageName = trackedImage.referenceImage.name;
            // Now loop over the array of prefabs
            foreach (GameObject curPrefab in ArPrefabs)
            {
                // Check whether this prefab matches the tracked image name, and that the prefab hasnt already been created
                if (string.Compare(curPrefab.name, imageName, StringComparison.OrdinalIgnoreCase) == 0
                    && !_instantiatedPrefabs.ContainsKey(imageName))
                {
                    // Instantiate the prefab, parenting it to the ARTrackedImage
                    GameObject newPrefab = Instantiate(curPrefab, trackedImage.transform);
                    // Add the created prefab to our array
                    _instantiatedPrefabs[imageName] = newPrefab;

                    // Check if the video player component exists
                    VideoPlayer videoPlayer = newPrefab.GetComponentInChildren<VideoPlayer>();
                    if (videoPlayer != null)
                    {
                        AddVideoPlayerEventListener(videoPlayer);
                    }
                }
            }
        }

        // For all prefabs that have been created so far, set them active or not depending
        // on whether their corresponding image is currently being tracked
        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            _instantiatedPrefabs[trackedImage.referenceImage.name].SetActive(trackedImage.trackingState == TrackingState.Tracking);

        }

        // If the AR subsystem has given up looking for a tracked image
        foreach (ARTrackedImage trackedImage in eventArgs.removed)
        {
            // Destroy its prefab
            Destroy(_instantiatedPrefabs[trackedImage.referenceImage.name]);
            // Also remove the instance from our array
            _instantiatedPrefabs.Remove(trackedImage.referenceImage.name);
            // Or, simply set the prefab instance to inactive
            //_instantiatedPrefabs[trackedImage.referenceImage.name].SetActive(false);

            // Access video player to pause it - DO MUTE ON CLICKING ON VIDEO PLAYER
            //_instantiatedPrefabs[trackedImage.referenceImage.name].gameObject.transform.GetChild(0).GetComponent<VideoPlayer>().Pause();
        }
    }

    private void AddVideoPlayerEventListener(VideoPlayer videoPlayer)
    {
        // Create an EventTrigger component if it doesn't exist
        EventTrigger eventTrigger = videoPlayer.gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            toastMessage.ShowMessage("ADDING");
            eventTrigger = videoPlayer.gameObject.AddComponent<EventTrigger>();
        }

        // Create a new entry for PointerClick event
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;

        // Add the callback function to the event trigger
        entry.callback.AddListener((data) => {
            Debug.Log("OnPointerDown called.");
            toastMessage.ShowMessage("CLICKING");
            videoPlayer.GetComponent<AudioSource>().mute = !videoPlayer.GetComponent<AudioSource>().mute;
        });

        // Add the entry to the event trigger
        eventTrigger.triggers.Add(entry);
    }
}