using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StaTech.AnimationValidator
{
    public class AnimationValidatorView : EditorWindow
    {
        private Texture2D _successIcon;
        private Texture2D SuccessIcon
        {
            get
            {
                if (_successIcon == null)
                {
                    _successIcon = GetBuiltInIcon("testpassed.png");
                }

                return _successIcon;
            }
        }

        private Texture2D _errorIcon;
        private Texture2D ErrorIcon
        {
            get
            {
                if (_errorIcon == null)
                {
                    _errorIcon = GetBuiltInIcon("testfailed.png");
                }

                return _errorIcon;
            }
        }

        private const float DetailSpace  = 30f;
        private const float RightPadding = 30f;

        private static List <ClipValidationContainer> _clipValidations;
        private static GameObject _selectedObject;
        private static AnimationValidatorView _window;
        private static Vector2 _scrollPos;

        [MenuItem("GameObject/アニメーションクリップ修正", false, -1)]
        public static void Open()
        {
            _clipValidations = AnimationValidator.ValidateAnimation();
            if (_clipValidations == null)
            {
                return;
            }
            _window              = GetWindow <AnimationValidatorView>();
            _window.titleContent = new GUIContent("anim修正");
            _selectedObject      = Selection.activeGameObject;
        }

        private void OnGUI()
        {
            if (_clipValidations == null || _clipValidations.Count == 0)
            {
                GUILayout.Label("AnimationClipがありません");

                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, true, true);
            GUILayout.Label("=====================================");
            GUILayout.Label("Animationでmissingになっているものを表示します");
            GUILayout.Label("オブジェクトの名前が他と被っていなれば自動で修正します");
            GUILayout.Label("=====================================");

            if (_clipValidations.Count > 1)
            {
                foreach (var result in _clipValidations)
                {
                    if (!result.HasNoError)
                    {
                        DrawButton("全部まとめて修正", () =>
                        {
                            AnimationValidator.ExecuteAllRecovery(_selectedObject, _clipValidations, ShowProgress);
                            _window.Repaint();
                            EditorApplication.RepaintAnimationWindow();
                        });
                        break;
                    }
                }
            }

            foreach (var result in _clipValidations)
            {
                DrawResult(result);
            }
            DrawButton("閉じる", () => _window.Close());
            EditorGUILayout.EndScrollView();
        }

        private void DrawResult(ClipValidationContainer result)
        {
            if (result.HasNoError)
            {
                using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(position.width - RightPadding)))
                {
                    DrawOnSuccess(result);
                }
            }
            else
            {
                using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(position.width - RightPadding)))
                {
                    DrawOnError(result);
                }
            }
        }

        private void DrawOnSuccess(ClipValidationContainer result)
        {
            DrawIconAndLabel(SuccessIcon, result.ClipName, "→正常です");
            DrawDetail(result.LostProperties);
        }

        private void DrawOnError(ClipValidationContainer result)
        {
            DrawIconAndLabel(ErrorIcon, result.ClipName);
            DrawDetail(result.LostProperties);
            DrawButton("自動修正", () =>
            {
                AnimationValidator.ExecuteUnitRecovery(_selectedObject, result, ShowProgress);
                _window.Repaint();
                EditorApplication.RepaintAnimationWindow();
            });
        }

        private void ShowProgress(string content, float progress)
        {
            EditorUtility.DisplayProgressBar("実行中", content, progress);
        }

        private void DrawDetail(List <LostProperty> Props)
        {
            using (new GUILayout.VerticalScope())
            {
                foreach (var prop in Props)
                {
                    var icon = prop.State == FixState.Fixed ? SuccessIcon : ErrorIcon;
                    DrawSpaceAndText(DetailSpace, prop.PropPath + " : " + prop.AttributeName, icon, prop.State);
                }
            }
        }

        private void DrawIconAndLabel(Texture image, string content, string detail="")
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(image, GUILayout.Height(18f), GUILayout.Width(20f));
                GUILayout.Label(content);
                if (!string.IsNullOrEmpty(detail))
                {
                    GUIStyle s = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = Color.green }
                    };
                    GUILayout.Label(detail, s);
                    GUILayout.FlexibleSpace();
                }
            }
        }

        private void DrawSpaceAndText(float space, string content, Texture headIcon, FixState state=FixState.None)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(space);
                if (headIcon)
                {
                    GUILayout.Label(headIcon, GUILayout.Height(18f), GUILayout.Width(20f));
                }
                GUILayout.Label(content);
                var detail = GetDetail(state);
                if (!string.IsNullOrEmpty(detail))
                {
                    // 黄色文字で足す
                    var color = GetColor(state);
                    if (color != Color.white)
                    {
                        GUIStyle s = new GUIStyle(EditorStyles.label)
                        {
                            normal = { textColor = color }
                        };
                        GUILayout.Label(detail, s);
                        GUILayout.FlexibleSpace();
                    }
                }
            }
        }

        private static Color GetColor(FixState state)
        {
            switch (state)
            {
                case FixState.ErrorDuplicate:
                case FixState.ErrorNoSameName:

                    return Color.yellow;
                case FixState.Fixed:

                    return Color.green;
                default:

                    return Color.white;
            }
        }

        private static string GetDetail(FixState state)
        {
            switch (state)
            {
                case FixState.ErrorDuplicate:

                    return "→同じ名前のオブジェクトが子階層上に複数あります";
                case FixState.ErrorNoSameName:

                    return "→同じ名前のオブジェクトが見つかりませんでした";
                case FixState.Lost:

                    return " ";
                case FixState.Fixed:

                    return "→Animationのパスを変更しました";

                default:

                    return "";
            }
        }

        private void DrawButton(string buttonName, System.Action callback=null)
        {
            if (GUILayout.Button(buttonName, GUILayout.Width(200f)))
            {
                if (callback != null)
                {
                    callback.Invoke();
                }
            }
        }

        private void OnDestroy()
        {
            if (_clipValidations != null)
            {
                _clipValidations.Clear();
                _clipValidations = null;
            }
        }

        private static Texture2D GetBuiltInIcon(string name)
        {
            System.Reflection.MethodInfo mi = typeof(EditorGUIUtility).GetMethod("IconContent", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new System.Type[] { typeof(string) }, null);
            if (mi == null) { mi = typeof(EditorGUIUtility).GetMethod("IconContent", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new System.Type[] { typeof(string) }, null); }

            return (Texture2D) ((GUIContent) mi.Invoke(null, new object[] { name })).image;
        }
    }
}