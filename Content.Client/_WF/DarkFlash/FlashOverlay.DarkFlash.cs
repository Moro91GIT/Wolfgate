using Content.Shared._WF.DarkFlash;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;

namespace Content.Client.Flash;

public sealed partial class FlashOverlay
{
    [Dependency] private IConfigurationManager _wfCfg = default!;

    private ShaderInstance? _wfStillShader;

    /// <summary>The still fade when the dark flash option is on, otherwise the usual shader.</summary>
    private ShaderInstance WfFlashShader()
    {
        if (!_wfCfg.GetCVar(DarkFlashCVars.Enabled))
            return _shader;

        return _wfStillShader ??= _prototypeManager.Index<ShaderPrototype>("WFFlashedEffectStill").InstanceUnique();
    }
}
