using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Adaptabrawl.UI
{
    /// <summary>
    /// Wraps almost everything except ASCII letters, digits, and ordinary space in TMP rich-text
    /// &lt;font&gt; tags so they render with <see cref="FallbackFontAssetName"/>. Trial fonts often
    /// watermark punctuation and symbols (e.g. &lt; &gt; - ( ) |) even though they are "ASCII";
    /// TMP will not use font fallbacks for glyphs present in the primary atlas, so this preprocessor
    /// forces Liberation Sans for those runs.
    /// </summary>
    public sealed class TrialFontUnicodeFallbackPreprocessor : ITextPreprocessor
    {
        public const string FallbackFontAssetName = "LiberationSans SDF";

        /// <summary>Shared instance — stateless, safe to reuse on all TMP_Text.</summary>
        public static readonly TrialFontUnicodeFallbackPreprocessor Instance = new TrialFontUnicodeFallbackPreprocessor();

        private TrialFontUnicodeFallbackPreprocessor()
        {
        }

        public string PreprocessText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var sb = new StringBuilder(text.Length + 32);
            int segmentStart = 0;
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '<' && TryConsumeRichTextTag(text, i, out int endExclusive))
                {
                    AppendWrappedRuns(sb, text, segmentStart, i);
                    sb.Append(text, i, endExclusive - i);
                    i = endExclusive;
                    segmentStart = i;
                    continue;
                }

                i++;
            }

            AppendWrappedRuns(sb, text, segmentStart, text.Length);
            return sb.ToString();
        }

        /// <summary>
        /// If <paramref name="openIndex"/> is '&lt;' and a matching '&gt;' closes a TMP rich-text tag
        /// (quotes respected), returns true and the index after '&gt;'. Otherwise false so '&lt;' is treated as a normal character.
        /// </summary>
        private static bool TryConsumeRichTextTag(string text, int openIndex, out int endExclusive)
        {
            endExclusive = openIndex;
            if (openIndex >= text.Length || text[openIndex] != '<')
                return false;

            if (!TryFindClosingAngleBracket(text, openIndex, out int afterClose))
                return false;

            // Reject "<>" / "<   >" so a lone "<" still gets fallback wrapping.
            int innerStart = openIndex + 1;
            int innerLen = afterClose - innerStart - 1;
            if (innerLen <= 0 || string.IsNullOrWhiteSpace(text.Substring(innerStart, innerLen)))
                return false;

            endExclusive = afterClose;
            return true;
        }

        private static bool TryFindClosingAngleBracket(string text, int openIndex, out int indexAfterClose)
        {
            indexAfterClose = openIndex;
            int j = openIndex + 1;
            bool inDoubleQuotes = false;
            while (j < text.Length)
            {
                char c = text[j];
                if (c == '"')
                    inDoubleQuotes = !inDoubleQuotes;
                else if (c == '>' && !inDoubleQuotes)
                {
                    indexAfterClose = j + 1;
                    return true;
                }

                j++;
            }

            return false;
        }

        private static void AppendWrappedRuns(StringBuilder sb, string text, int start, int end)
        {
            if (start >= end)
                return;

            int i = start;
            while (i < end)
            {
                int cpLen = char.IsSurrogatePair(text, i) ? 2 : 1;
                if (i + cpLen > end)
                    cpLen = 1;

                if (!NeedsFallback(text, i, cpLen))
                {
                    sb.Append(text, i, cpLen);
                    i += cpLen;
                    continue;
                }

                int segStart = i;
                i += cpLen;
                while (i < end)
                {
                    int len = char.IsSurrogatePair(text, i) ? 2 : 1;
                    if (i + len > end)
                        len = 1;
                    if (!NeedsFallback(text, i, len))
                        break;
                    i += len;
                }

                sb.Append("<font=\"");
                sb.Append(FallbackFontAssetName);
                sb.Append("\">");
                // A run that is only '<' would otherwise become <font><</font> and TMP treats the inner '<' as
                // a tag opener. noparse only when the wrapped segment is exactly that character.
                if (i - segStart == 1 && text[segStart] == '<')
                    sb.Append("<noparse><</noparse>");
                else
                    sb.Append(text, segStart, i - segStart);
                sb.Append("</font>");
            }
        }

        /// <summary>
        /// Only ASCII A–Z, a–z, 0–9, and space stay on the trial font. All other characters (including
        /// every punctuation/symbol in 0x20–0x7E such as &lt; &gt; - ( ) |) use the fallback font.
        /// Line breaks and tab are left unwrapped (no &lt;font&gt; tag).
        /// </summary>
        private static bool NeedsFallback(string text, int index, int codeUnitLength)
        {
            if (codeUnitLength == 2)
                return true;

            char c = text[index];
            if (c == '\n' || c == '\r' || c == '\t')
                return false;

            if (c >= 'A' && c <= 'Z')
                return false;
            if (c >= 'a' && c <= 'z')
                return false;
            if (c >= '0' && c <= '9')
                return false;
            if (c == ' ')
                return false;

            return true;
        }
    }

    /// <summary>
    /// Assigns <see cref="TrialFontUnicodeFallbackPreprocessor.Instance"/> to TMP_Text that use trial fonts.
    /// </summary>
    public static class TrialFontFallbackBootstrap
    {
        /// <summary>Disable for debugging or if you replace trial fonts with licensed assets.</summary>
        public static bool Enabled = true;

        private const int FramesToRescan = 6;

        private static int framesLeft;

        /// <summary>Call when creating TMP_Text at runtime so the preprocessor is applied.</summary>
        public static void Register(TMP_Text tmp)
        {
            if (!Enabled || tmp == null)
                return;
            ApplyTo(tmp);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            var go = new GameObject(nameof(TrialFontFallbackBootstrap));
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }

        private sealed class Runner : MonoBehaviour
        {
            private void Awake()
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.activeSceneChanged += OnActiveSceneChanged;
                RequestRescan();
                ApplyToAll();
            }

            private void OnDestroy()
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            }

            private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                RequestRescan();
            }

            private static void OnActiveSceneChanged(Scene previous, Scene next)
            {
                RequestRescan();
            }

            private void Update()
            {
                if (framesLeft <= 0)
                    return;
                framesLeft--;
                ApplyToAll();
            }

            private void Start()
            {
                RequestRescan();
            }
        }

        private static void RequestRescan()
        {
            framesLeft = FramesToRescan;
        }

        private static void ApplyToAll()
        {
            if (!Enabled)
                return;

            var tmpObjects = UnityEngine.Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < tmpObjects.Length; i++)
                ApplyTo(tmpObjects[i]);
        }

        private static void ApplyTo(TMP_Text tmp)
        {
            if (tmp == null)
                return;

            if (!IsTrialFont(tmp.font))
            {
                if (tmp.textPreprocessor == TrialFontUnicodeFallbackPreprocessor.Instance)
                    tmp.textPreprocessor = null;
                return;
            }

            if (tmp.textPreprocessor != null && tmp.textPreprocessor != TrialFontUnicodeFallbackPreprocessor.Instance)
                return;

            tmp.textPreprocessor = TrialFontUnicodeFallbackPreprocessor.Instance;
        }

        /// <summary>True if the assigned font is a known trial / demo asset name.</summary>
        public static bool IsTrialFont(TMP_FontAsset font)
        {
            if (font == null)
                return false;

            string name = font.name;
            return name.IndexOf("Trial", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("-trial", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
