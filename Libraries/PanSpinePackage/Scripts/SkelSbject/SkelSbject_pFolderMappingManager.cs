using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Spine;
using Spine.Unity;
using System.Text;
using Pan.Util;
using System.Linq;
using Pan.SpinePackage;
using static Pan.SpinePackage.SkelObject.AniCore.Players;
using System.Runtime.CompilerServices;



//? 스파인의 폴더 경로를 열거형으로 미리 지정하여 편하게 불러올수있게 하는 매니저 가 있는 정도의 코드



namespace Pan.SpinePackage
{
    public partial class SkelSbject
    {
        ///======================================================================================================================================================



        //? 베이스



        /// <summary>
        /// 열거형 타입으로 폴더의 이름을 단축하여
        /// <para>해당 <typeparamref name="TEnumFolder"/>를 해당 폴더 경로로 변환시키는 매니저</para>
        /// <para><b>생성자에서 모든 것이 정해짐!</b></para>
        /// </summary>
        /// <typeparam name="TEnumFolder">열거형 폴더 타입</typeparam>
        public abstract class FolderMappingManagerBase<TEnumFolder> where TEnumFolder : struct, Enum
        {
            /// <summary>
            /// 이 생성자에서 변환 타입이 정해진다<br/>
            /// <paramref name="getFolderPathEvent"/>는 null 이여서는 절대 안된다<br/>
            /// <paramref name="getFolderPathEvent"/>를 switch문으로 작성하는것을 권장한다
            /// </summary>
            /// <param name="skelSbject"></param>
            /// <param name="getFolderPathEvent"> 열거형을 인자로 받아와, string을 반환해야한다<br/> switch문 작성 권장</param>
            /// <exception cref="Exception"><paramref name="getFolderPathEvent"/>가 null일경우 반환됨</exception>
            public FolderMappingManagerBase(SkelSbject skelSbject, Func<TEnumFolder, string> getFolderPathEvent)
            {
                SkelSbject = skelSbject;
                GetFolderPathEvent = getFolderPathEvent;

                if (SkelSbject == null || GetFolderPathEvent == null)
                {
                    throw new Exception($"({GetType()}) GetFolderPathEvent또는 SkelSbject가 null값이 지정됨! 반드시 생성자에서 직접 지정할것");
                }
            }



            /// <summary>
            /// 이곳에서 사용되는 DB
            /// </summary>
            protected readonly SkelSbject SkelSbject;

            /// <summary>
            /// 생성자에서 지정되는 열거형-문자열 변환 델리게이트
            /// </summary>
            protected readonly Func<TEnumFolder, string> GetFolderPathEvent;



            /// <summary>
            /// 열거형 폴더를 문자열로 변환하여 반환한다
            /// </summary>
            /// <param name="enumFolder">해당 폴더 (열거형)</param>
            public string ConvertFolder(TEnumFolder enumFolder) => GetFolderPathEvent.Invoke(enumFolder);

            /// <summary>
            /// 열거형 폴더를 문자열로 변환하여, 문자열과 합쳐 반환한다
            /// </summary>
            /// <param name="enumFolder">해당 폴더 (열거형)</param>
            /// <param name="concatName">합칠 문자열</param>
            public string ConvertFolder(TEnumFolder enumFolder, string concatName) => string.Concat(GetFolderPathEvent.Invoke(enumFolder), concatName);
        }



        ///======================================================================================================================================================



        //? 애니메이션 폴더



        /// <summary>
        /// 애니메이션 폴더 매핑 매니저 인터페이스
        /// </summary>
        /// <typeparam name="TAniFolder">해당 애니메이셜 폴더 타입 (열거형)</typeparam>
        public interface IAnimationFolderMappingManager<TAniFolder> where TAniFolder : struct, Enum
        {
            /// <summary>
            /// 대상 애니메이션 폴더 매핑 매니저
            /// </summary>
            AnimationFolderMappingManager<TAniFolder> AniFolder { get; }

            /// <summary>
            /// 애니메이션 폴더 이름 얻기 (열거형 폴더 지원!)
            /// </summary>
            /// <param name="aniFolder">애니메이션 폴더 (열거형)</param>
            /// <returns></returns>
            public string GetAnimationFolderName(TAniFolder aniFolder) => AniFolder.ConvertFolder(aniFolder);

            /// <summary>
            /// 애니메이션 얻기 (열거형 폴더 지원!)
            /// </summary>
            /// <param name="aniFolder">애니메이션 폴더 (열거형)</param>
            /// <param name="animationName">애니메이션 이름</param>
            /// <returns></returns>
            public Spine.Animation GetAnimation(TAniFolder aniFolder, string animationName) => AniFolder.GetAnimation(aniFolder, animationName);
        }



        /// <summary>
        /// 애니메이션 폴더 매핑 매니저
        /// </summary>
        /// <typeparam name="TAniFolder">해당 애니메이셜 폴더 타입 (열거형)</typeparam>
        public class AnimationFolderMappingManager<TAniFolder> : FolderMappingManagerBase<TAniFolder>, IAnimationFolderMappingManager<TAniFolder> where TAniFolder : struct, Enum
        {
            /// <inheritdoc/>
            public AnimationFolderMappingManager(SkelSbject skelSbject, Func<TAniFolder, string> getFolderPathEvent) : base(skelSbject, getFolderPathEvent) { }

            AnimationFolderMappingManager<TAniFolder> IAnimationFolderMappingManager<TAniFolder>.AniFolder => this;

            /// <inheritdoc/>
            public Spine.Animation GetAnimation(TAniFolder aniFolder, string animationName)
            {
                return SkelSbject.GetAnimation(ConvertFolder(aniFolder, animationName));
            }
        }



        ///======================================================================================================================================================



        //? 스킨 폴더



        /// <summary>
        /// 스킨 폴더 매핑 매니저 인터페이스
        /// </summary>
        /// <typeparam name="TAniFolder">해당 애니메이셜 폴더 타입 (열거형)</typeparam>
        public interface ISkinFolderMappingManager<TSkinFolder> where TSkinFolder : struct, Enum
        {
            /// <summary>
            /// 대상 스킨 폴더 매핑 매니저
            /// </summary>
            SkinFolderMappingManager<TSkinFolder> SkinFolder { get; }



            /// <summary>
            /// 스킨 이름 얻기 (열거형 폴더 지원!)
            /// </summary>
            /// <param name="skinFolder">스킨 폴더 (열거형)</param>
            /// <returns></returns>
            public string GetSkinName(TSkinFolder skinFolder, string skinName) => SkinFolder.ConvertFolder(skinFolder, skinName);



            /// <summary>
            /// 애니메이션 얻기 (열거형 폴더 지원!)
            /// </summary>
            /// <param name="skinFolder">애니메이션 폴더 (열거형)</param>
            /// <param name="animationName">애니메이션 이름</param>
            /// <returns></returns>
            public Skin GetSkin(TSkinFolder skinFolder, string skinName) => SkinFolder.GetSkin(skinFolder, skinName);
        }



        /// <summary>
        /// 스킨 폴더 매핑 매니저
        /// </summary>
        /// <typeparam name="TAniFolder">해당 애니메이셜 폴더 타입 (열거형)</typeparam>
        public class SkinFolderMappingManager<TSkinFolder> : FolderMappingManagerBase<TSkinFolder>, ISkinFolderMappingManager<TSkinFolder> where TSkinFolder : struct, Enum
        {
            /// <inheritdoc/>
            public SkinFolderMappingManager(SkelSbject skelSbject, Func<TSkinFolder, string> getFolderPathEvent) : base(skelSbject, getFolderPathEvent) { }

            SkinFolderMappingManager<TSkinFolder> ISkinFolderMappingManager<TSkinFolder>.SkinFolder => this;

            /// <inheritdoc/>
            public Skin GetSkin(TSkinFolder skinFolder, string skinName)
            {
                return SkelSbject.GetSkin(ConvertFolder(skinFolder, skinName));
            }
        }



        ///======================================================================================================================================================
    }
}