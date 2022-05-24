
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.Components.Video;
using VRC.Udon.Common.Interfaces;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(-1)]
    public class Queue : UdonSharpBehaviour
    {
        private const string TSTR_QUEUE_LIMIT_REACHED = "Queue Limit Reached";
        private const string TSTR_PLAYER_LIMIT_REACHED = "Personal Limit Reached";

        public TVManagerV2 tv;
        public RectTransform listContainer;
        // public GameObject template;
        public VRCUrlInputField urlInput;
        public Button queueMedia;
        public Button nextMedia;
        public InputField titleInput;
        public Text toasterMsg;
        public byte maxVideosPerPlayer = 2;
        [HideInInspector] public const int maxQueuedVideos = 10;
        private Text[] urlDisplays = new Text[maxQueuedVideos];
        private Text[] titleDisplays = new Text[maxQueuedVideos];
        private Text[] ownerDisplays = new Text[maxQueuedVideos];

        private int OUT_TvOwnerChange_int_Id;
        private VideoError OUT_TvVideoPlayerError_VideoError_Error;
        private Slider loadingBar;
        private float loadingBarDamp;
        private bool isLoading = false;

        [UdonSynced] private VRCUrl[] urls = new VRCUrl[maxQueuedVideos];
        [UdonSynced] private string[] titles = new string[maxQueuedVideos];
        [UdonSynced] private int[] owners = new int[maxQueuedVideos];
        private Button[] removal = new Button[maxQueuedVideos];

        private bool hasLoadingBar;
        private bool hasTitleInput;
        private bool hasToaster;
        private bool requestedByMe = false;
        private const string EMPTY = "";
        private bool noActiveMedia = true;
        private VRCPlayerApi localPlayer;
        private bool hasLocalPlayer;
        private bool init = false;
        private bool debug = true;
        private string debugColor = "yellow";

        public void _Initialize()
        {
            if (init) return;
            if (tv == null) tv = transform.parent.GetComponent<TVManagerV2>();
            for (int i = 0; i < maxQueuedVideos; i++)
            {
                var t = listContainer.GetChild(i);
                if (t == null) break;
                var ownerT = t.Find("Owner");
                var titleT = t.Find("Title");
                var urlT = t.Find("Url");
                var removeT = t.Find("Remove");
                if (urlT != null) urlDisplays[i] = urlT.GetComponent<Text>();
                if (titleT != null) titleDisplays[i] = titleT.GetComponent<Text>();
                if (ownerT != null) ownerDisplays[i] = ownerT.GetComponent<Text>();
                if (removeT != null)
                {
                    removal[i] = removeT.GetComponent<Button>();
                    removeT.gameObject.SetActive(false);
                }
                titles[i] = EMPTY;
                urls[i] = VRCUrl.Empty;
            }
            loadingBar = listContainer.GetChild(0).GetComponentInChildren<Slider>();
            localPlayer = Networking.LocalPlayer;
            hasLocalPlayer = localPlayer != null;
            hasLoadingBar = loadingBar != null;
            hasTitleInput = titleInput != null;
            hasToaster = toasterMsg != null;
            _UpdateUrlInput();
            updateUI();
            // this plugin's priority should be higher than most other plugins
            tv._RegisterUdonSharpEventReceiverWithPriority(this, 100);
            init = true;
        }

        void Start()
        {
            _Initialize();
        }

        void LateUpdate()
        {
            if (isLoading) if (hasLoadingBar)
                {
                    if (loadingBar.value > 0.95f) return;
                    if (loadingBar.value > 0.8f)
                        loadingBar.value = Mathf.SmoothDamp(loadingBar.value, 1f, ref loadingBarDamp, 0.4f);
                    else
                        loadingBar.value = Mathf.SmoothDamp(loadingBar.value, 1f, ref loadingBarDamp, 0.3f);
                }
        }

        new void OnPostSerialization(SerializationResult result)
        {
            if (result.success) { }
            else {
                RequestSerialization();
            }
        }

        new void OnDeserialization()
        {
            updateUI();
        }

        new void OnPlayerLeft(VRCPlayerApi player) {
            if (isTVOwner()) {
                var pid = localPlayer.playerId;
                var oldpid = player.playerId;
                for (int i = 0; i < owners.Length; i++) {
                    if (oldpid == owners[i]) {
                        owners[i] = pid;
                    }
                }
                updateUI();
            }
        }


        // === UI Events ===
        public void _UpdateUrlInput()
        {
            if (urlInput.GetUrl().Get() == string.Empty)
                queueMedia.gameObject.SetActive(false);
            else queueMedia.gameObject.SetActive(true);
        }

        public void _QueueMedia()
        {
            VRCUrl urlIn = urlInput.GetUrl();
            urlInput.SetUrl(VRCUrl.Empty);
            string titleIn = EMPTY;
            if (hasTitleInput)
            {
                titleIn = titleInput.text ?? EMPTY;
                titleInput.text = EMPTY;
            }
            int target = -1;
            for (int i = maxQueuedVideos - 1; i >= 0; i--)
            {
                var url = urls[i];
                url = url ?? VRCUrl.Empty;
                if (url.Get() != EMPTY) break; // find the first entry that is empty
                target = i;
            }
            if (target == -1)
            {
                log("Queue is full. Wait until another video has been cleared.");
                return;
            }
            log($"QueueMedia: Assigning new media to entry {target}");
            Networking.SetOwner(localPlayer, gameObject);
            urls[target] = urlIn;
            titles[target] = titleIn;
            owners[target] = localPlayer.playerId;
            // TODO fix highlight on queue?
            // TODO Verify this cause I think it's fixed...
            if (hasLoadingBar && target == 0)
                loadingBar.value = urls[0].Get() == tv.url.Get() ? 1f : 0f;
            collapseEntries();
            if (noActiveMedia) play();
        }

        public void _Remove()
        {
            var index = getInteractedIndex();
            if (index > -1 && (localPlayer.playerId == owners[index] || isMasterOwner()))
            {
                Networking.SetOwner(localPlayer, gameObject);
                if (index == 0 && matchCurrentUrl(true))
                {
                    tv._Stop();
                    requestNext();
                }
                else
                {
                    urls[index] = VRCUrl.Empty;
                    collapseEntries();
                }
            }
        }

        public void _Next() => requestNext();


        // ======== NETWORK METHODS ===========

        private void requestNext()
        {
            requestedByMe = true;
            if (!tv.locked || isTVOwner())
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_RequestNext));
            }
        }

        public void ALL_RequestNext() // PUT ALL Next BUTTON CHECKS HERE
        {
            if (tv.loading || !hasLocalPlayer) return; // ignore any requests for next while loading to prevent next spamming
            if (tv.locked && isTVOwner())
            { // only allow the TV owner to act if the tv is locked
                if (requestedByMe)
                { // only allow self-requested NEXT calls when the TV is locked.
                    if (matchCurrentUrl(true))
                    { // if the current url is in the TV, switch to the next URL
                        Networking.SetOwner(localPlayer, gameObject);
                        nextURL();
                    }
                    play();
                }
            }
            else
            {
                if (matchCurrentUrl(true))
                { // if the current url is in the TV
                    if (isOwner() && checkNextUrl(false) || localPlayer.playerId == owners[1])
                    {
                        // allow pass through for queue owner if there isn't another media in queue
                        //      This allows for media end to clear the last url from the queue
                        // update owner of queue to the owner of next queued media otherwise
                        Networking.SetOwner(localPlayer, gameObject);
                        nextURL();
                        play();
                    }
                }
                else
                {
                    if (checkCurrentUrl(true) && localPlayer.playerId == owners[0])
                    {
                        // if there is a URL in the queue, make the owner of that queue entry play the video
                        play();
                    }
                }
            }
            requestedByMe = false;
        }


        // === TV Events ===

        public void _TvMediaChange()
        {
            // if the tv media changes and the current URL does not match, collapse the entries
            if (isOwner() && matchCurrentUrl(false))
                collapseEntries();
        }

        public void _TvMediaEnd()
        {
            noActiveMedia = true;

            if (isTVOwner())
            {
                if (matchCurrentUrl(true)) // urls[0] matches the tv url
                    requestNext(); // attempt queueing the next media
                else play(); // attempt to play urls[0]
            }
        }

        public void _TvMediaStart()
        {
            // track active media state and update the TV label if the current media has a title associated with it
            noActiveMedia = false;
            if (titles[0] != EMPTY)
                tv.localLabel = titles[0];
        }

        public void _TvMediaLoop()
        {
            // enforce active media state when media loops
            noActiveMedia = false;
        }

        public void _TvVideoPlayerError()
        {
            if (OUT_TvVideoPlayerError_VideoError_Error == VideoError.RateLimited) return; // TV auto-reloads on ratelimited, don't skip current media.
            noActiveMedia = true;
        }

        public void _TvLoading()
        {
            // ONLY enable loading if the current url matches
            if (hasLoadingBar) loadingBar.value = 0f;
            if (matchCurrentUrl(true))
                isLoading = true;
        }

        public void _TvLoadingEnd()
        {
            if (isLoading)
            {
                isLoading = false;
                if (hasLoadingBar) loadingBar.value = 1f;
            }
        }

        public void _TvLoadingAbort()
        {
            isLoading = false;
            if (hasLoadingBar) loadingBar.value = 0f;
        }

        public void _TvLock()
        {
            nextMedia.gameObject.SetActive(isTVOwner());
        }

        public void _TvUnLock()
        {
            nextMedia.gameObject.SetActive(true);
        }

        // ======== HELPER METHODS ============


        private void playNext()
        {
            if (matchCurrentUrl(true)) nextURL();
            play();
        }

        private void nextURL()
        {
            if (isOwner())
            {
                urls[0] = VRCUrl.Empty;
                if (hasLoadingBar) loadingBar.value = 0f;
                collapseEntries();
            }
        }

        private void play()
        {
            if (urls[0].Get() != EMPTY)
            {
                noActiveMedia = false;
                log($"Next URL - {urls[0]} | title '{titles[0]}'");
                tv._ChangeMediaTo(urls[0]);
            }
        }

        private bool urlMatch(VRCUrl check, bool shouldMatch)
        {
            check = check ?? VRCUrl.Empty;
            bool matches = tv.url.Get() == check.Get();
            return matches == shouldMatch;
        }

        private bool urlExist(VRCUrl check, bool shouldExist)
        {
            check = check ?? VRCUrl.Empty;
            bool exists = check != VRCUrl.Empty;
            return exists == shouldExist;
        }


        private bool matchCurrentUrl(bool shouldMatch) => urlMatch(urls[0], shouldMatch);
        private bool checkCurrentUrl(bool shouldExist) => urlExist(urls[0], shouldExist);
        private bool checkNextUrl(bool shouldExist) => urlExist(urls[1], shouldExist);

        private void collapseEntries()
        {
            var _urls = new VRCUrl[maxQueuedVideos];
            var _titles = new string[maxQueuedVideos];
            var _owners = new int[maxQueuedVideos];
            int index = 0;
            for (int i = 0; i < maxQueuedVideos; i++)
            {
                var url = urls[i];
                url = url ?? VRCUrl.Empty;
                _urls[index] = url;
                if (url.Get() == EMPTY)
                {
                    titles[i] = EMPTY;
                    owners[i] = -1;
                    continue;
                }
                _titles[index] = titles[i];
                _owners[index] = owners[i];
                index++;
            }
            for (int i = index; i < maxQueuedVideos; i++) {
                // fill the remainder of the arrays with default values
                _urls[i] = VRCUrl.Empty;
                _titles[i] = EMPTY;
                _owners[i] = -1;
            }
            log($"Updated to {index} entries");
            urls = _urls;
            titles = _titles;
            owners = _owners;
            RequestSerialization();
            updateUI();
        }

        private void updateUI()
        {
            if (hasToaster)
                toasterMsg.text = EMPTY;
            var masterOwner = isMasterOwner();
            int count = 0;
            int personalCount = 0;
            for (int i = 0; i < maxQueuedVideos; i++)
            {
                var url = urls[i];
                var title = titles[i];
                url = url ?? VRCUrl.Empty;
                if (url.Get() == EMPTY)
                {
                    listContainer.GetChild(i).gameObject.SetActive(false);
                    continue;
                }
                var owner = VRCPlayerApi.GetPlayerById(owners[i]);
                var hasCustomTitle = title != EMPTY;
                if (!Utilities.IsValid(owner)) return; // invalid player
                if (ownerDisplays[i] != null)
                {
                    if (debug)
                        ownerDisplays[i].text = $"{owner.displayName} [{owner.playerId}]";
                    else
                        ownerDisplays[i].text = owner.displayName;
                }
                if (titleDisplays[i] != null)
                    titleDisplays[i].text = title;
                if (urlDisplays[i] != null)
                {
                    if (hasCustomTitle)
                    {
                        urlDisplays[i].text = url.Get();
                    }
                    else
                    {
                        titleDisplays[i].text = url.Get();
                        urlDisplays[i].text = EMPTY;
                    }
                }
                var remove = removal[i];
                if (remove != null)
                {
                    var isOwner = localPlayer == owner;
                    remove.gameObject.SetActive(isOwner || masterOwner);
                    if (isOwner) personalCount++;
                }
                listContainer.GetChild(i).gameObject.SetActive(true);
                count++;
            }
            if (hasLoadingBar && urls[0].Get() != tv.url.Get())
                loadingBar.value = 0f;

            if (count >= maxQueuedVideos)
            {
                urlInput.SetUrl(VRCUrl.Empty);
                urlInput.gameObject.SetActive(false);
                titleInput.gameObject.SetActive(false);
                if (hasToaster) toasterMsg.text = TSTR_QUEUE_LIMIT_REACHED;
            }
            else if (!masterOwner && personalCount >= maxVideosPerPlayer) {
                urlInput.SetUrl(VRCUrl.Empty);
                urlInput.gameObject.SetActive(false);
                titleInput.gameObject.SetActive(false);
                if (hasToaster) toasterMsg.text = TSTR_PLAYER_LIMIT_REACHED;
            }
            else {
                urlInput.gameObject.SetActive(true);
                titleInput.gameObject.SetActive(true);
                if (hasToaster) toasterMsg.text = "";
            }

            if (tv.locked) nextMedia.gameObject.SetActive(isTVOwner());
            else nextMedia.gameObject.SetActive(true);
        }

        private int getInteractedIndex()
        {
            for (int i = 0; i < maxQueuedVideos; i++)
            {
                if (removal[i].interactable) { }
                else return i;
            }
            return -1;
        }



        private bool isOwner() => Networking.IsOwner(localPlayer, gameObject);
        private bool isMasterOwner() => hasLocalPlayer && (tv.allowMasterControl && localPlayer.isMaster || localPlayer.isInstanceOwner);
        private bool isTVOwner() => Networking.IsOwner(localPlayer, tv.gameObject);

        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(Queue)}</color>] {value}");
        }
        private void warn(string value)
        {
            if (debug) Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(Queue)}</color>] {value}");
        }
        private void err(string value)
        {
            if (debug) Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(Queue)}</color>] {value}");
        }
    }
}
