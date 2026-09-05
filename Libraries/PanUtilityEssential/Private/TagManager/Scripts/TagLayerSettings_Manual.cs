using UnityEngine;



namespace Pan.Util
{
    ///======================================================================================================================================================



    public static partial class SU_Tags
    {

    }



    ///======================================================================================================================================================



    public static partial class SU_Layers
    {
#if PAN_LAYERSETTING_DANMAKU

        public static Color GetColor_ByCollisionLayers(int layer)
        {
            if (IsLayer(layer, EL.Player) && !IsLayer(layer, EL.Enemy) && !IsLayer(layer, EL.Neutrality))
            {
                return Color.blue;
            }
            else if (!IsLayer(layer, EL.Player) && IsLayer(layer, EL.Enemy) && !IsLayer(layer, EL.Neutrality))
            {
                return Color.red;
            }
            else if (!IsLayer(layer, EL.Player) && !IsLayer(layer, EL.Enemy) && IsLayer(layer, EL.Neutrality))
            {
                return Color.green;
            }
            else if (IsLayer(layer, EL.Player) && IsLayer(layer, EL.Enemy) && !IsLayer(layer, EL.Neutrality))
            {
                return Color.magenta;
            }
            else if (IsLayer(layer, EL.Player) && !IsLayer(layer, EL.Enemy) && IsLayer(layer, EL.Neutrality))
            {
                return Color.cyan;
            }
            else if (!IsLayer(layer, EL.Player) && IsLayer(layer, EL.Enemy) && IsLayer(layer, EL.Neutrality))
            {
                return Color.yellow;
            }
            else
            {
                return Color.white;
            }
        }

#endif
    }



    ///======================================================================================================================================================



    public static partial class SU_SortingLayers
    {

    }



    ///======================================================================================================================================================



    public static partial class SU_RenderingLayers
    {

    }



    ///======================================================================================================================================================
}