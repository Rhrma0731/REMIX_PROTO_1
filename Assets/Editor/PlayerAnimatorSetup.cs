using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.Linq;

/// <summary>
/// Tools > Setup Player Animator
/// - Player_front_Walk.anim / Player_side_Walk.anim / Player_back_Walk.anim 생성 (8fps, Loop)
/// - Player_Animator.controller 생성
///     · WalkState (int) 파라미터: 0=front, 1=side, 2=back
///     · 3개 상태 간 완전 전환 (즉시, hasExitTime=false)
///     · 기본 상태: Player_front_Walk
/// - 씬의 GlitchDuck에 Animator + PlayerAnimationController 컴포넌트 추가 및 컨트롤러 할당
/// </summary>
public static class PlayerAnimatorSetup
{
    const string ANIM_FOLDER = "Assets/Animations";
    const string FRONT_GUID  = "99444c601ec2b0e45acaeb63449ba4aa"; // walk_front.png
    const string SIDE_GUID   = "7ab46f9fd3403c149ba4db47f73554fc"; // walk_side-Sheet.png
    const string BACK_GUID   = "15ac4340075954d4da41db1f29a19e97"; // walk_back-Sheet.png
    const string SPRITE_PATH = "Body";

    // WalkState 값 — PlayerAnimationController와 반드시 일치해야 함
    const int STATE_FRONT = 0;
    const int STATE_SIDE  = 1;
    const int STATE_BACK  = 2;

    [MenuItem("Tools/Setup Player Animator")]
    static void Run()
    {
        // ── 0. Animations 폴더 보장 ─────────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder(ANIM_FOLDER))
            AssetDatabase.CreateFolder("Assets", "Animations");

        // ── 1. 애니메이션 클립 3개 생성 ─────────────────────────────────────────
        var frontClip = BuildClip("Player_front_Walk", FRONT_GUID);
        var sideClip  = BuildClip("Player_side_Walk",  SIDE_GUID);
        var backClip  = BuildClip("Player_back_Walk",  BACK_GUID);

        if (frontClip == null || sideClip == null || backClip == null)
        {
            Debug.LogError("[PlayerAnimatorSetup] 스프라이트 로드 실패. 작업 중단.");
            return;
        }

        SaveOrReplace(frontClip, ANIM_FOLDER + "/Player_front_Walk.anim");
        SaveOrReplace(sideClip,  ANIM_FOLDER + "/Player_side_Walk.anim");
        SaveOrReplace(backClip,  ANIM_FOLDER + "/Player_back_Walk.anim");

        // ── 2. Animator Controller 생성 ─────────────────────────────────────────
        string ctrlPath = ANIM_FOLDER + "/Player_Animator.controller";
        AssetDatabase.DeleteAsset(ctrlPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);

        // WalkState int 파라미터 (0=front, 1=side, 2=back)
        controller.AddParameter("WalkState", AnimatorControllerParameterType.Int);

        var sm = controller.layers[0].stateMachine;

        // 3개 상태 추가
        var frontState = sm.AddState("Player_front_Walk");
        frontState.motion = frontClip;
        sm.defaultState = frontState;   // 기본 상태

        var sideState = sm.AddState("Player_side_Walk");
        sideState.motion = sideClip;

        var backState = sm.AddState("Player_back_Walk");
        backState.motion = backClip;

        // 전환 추가 — 6방향 모두 (hasExitTime=false, duration=0, 즉시 전환)
        AddTransition(frontState, sideState, "WalkState", STATE_SIDE);
        AddTransition(frontState, backState, "WalkState", STATE_BACK);

        AddTransition(sideState,  frontState, "WalkState", STATE_FRONT);
        AddTransition(sideState,  backState,  "WalkState", STATE_BACK);

        AddTransition(backState,  frontState, "WalkState", STATE_FRONT);
        AddTransition(backState,  sideState,  "WalkState", STATE_SIDE);

        AssetDatabase.SaveAssets();
        Debug.Log("[PlayerAnimatorSetup] Player_Animator.controller 생성 완료 " +
                  "(WalkState int 파라미터, 3-state 전환).");

        // ── 3. GlitchDuck에 Animator + PlayerAnimationController 추가 ───────────
        var duck = GameObject.Find("GlitchDuck");
        if (duck == null)
        {
            Debug.LogError("[PlayerAnimatorSetup] 씬에서 'GlitchDuck'을 찾을 수 없습니다.");
            return;
        }

        var animator = duck.GetComponent<Animator>();
        if (animator == null)
        {
            animator = duck.AddComponent<Animator>();
            Debug.Log("[PlayerAnimatorSetup] GlitchDuck에 Animator 컴포넌트 추가.");
        }
        else
        {
            Debug.Log("[PlayerAnimatorSetup] 기존 Animator에 컨트롤러 재할당.");
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        if (duck.GetComponent<PlayerAnimationController>() == null)
        {
            duck.AddComponent<PlayerAnimationController>();
            Debug.Log("[PlayerAnimatorSetup] GlitchDuck에 PlayerAnimationController 추가.");
        }

        EditorSceneManager.MarkSceneDirty(duck.scene);
        AssetDatabase.Refresh();
        Debug.Log("[PlayerAnimatorSetup] 완료!");
    }

    // ── 헬퍼: 즉시 전환 추가 ──────────────────────────────────────────────────
    static void AddTransition(AnimatorState from, AnimatorState to, string param, int value)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration    = 0f;
        t.AddCondition(AnimatorConditionMode.Equals, value, param);
    }

    // ── 헬퍼: 애니메이션 클립 생성 ────────────────────────────────────────────
    static AnimationClip BuildClip(string clipName, string textureGuid)
    {
        string assetPath = AssetDatabase.GUIDToAssetPath(textureGuid);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError($"[PlayerAnimatorSetup] GUID '{textureGuid}'에 해당하는 에셋 없음.");
            return null;
        }

        var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .OrderBy(s =>
            {
                var parts = s.name.Split('_');
                return int.TryParse(parts[parts.Length - 1], out int n) ? n : 0;
            })
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError($"[PlayerAnimatorSetup] '{assetPath}'에서 Sprite를 찾을 수 없음. " +
                           "Sprite Mode가 Multiple인지 확인하세요.");
            return null;
        }

        var clip = new AnimationClip
        {
            name      = clipName,
            frameRate = 8f,
            wrapMode  = WrapMode.Loop
        };

        var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

        var binding = new EditorCurveBinding
        {
            path         = SPRITE_PATH,
            type         = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        var keys = sprites.Select((s, i) => new ObjectReferenceKeyframe
        {
            time  = i / 8f,
            value = s
        }).ToArray();

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        Debug.Log($"[PlayerAnimatorSetup] '{clipName}' 생성 ({sprites.Length}프레임).");
        return clip;
    }

    // ── 헬퍼: 기존 에셋 삭제 후 저장 ─────────────────────────────────────────
    static void SaveOrReplace(AnimationClip clip, string path)
    {
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
    }
}
