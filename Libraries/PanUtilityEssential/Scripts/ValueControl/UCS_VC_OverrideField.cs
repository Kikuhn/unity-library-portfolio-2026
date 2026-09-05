using System;
using UnityEngine;



//? [ValueControl] OverrideField 가 정리되어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    /// <summary>
    /// 필드를 읽어올때, 원본 필드, 재정의된 필드, 자동으로 전환하여 반환하는 필드를 정의하는 열거형
    /// </summary>
    public enum EOverrideFieldType
    {
        /// <summary>원본 필드</summary>
        Original,
        /// <summary>재정의된 필드</summary>
        Overridden,
        /// <summary>자동으로 반환되는 필드</summary>
        Auto
    }



    /// <summary>
    /// OverrideFieldManager 클래스는 TOverrideField 타입의 원본 필드와 재정의된 필드를 관리합니다.
    /// </summary>
    /// <typeparam name="TOverrideField"><see cref="BaseOverrideField{TCurrent}"/>를 상속받는 타입</typeparam>
    [Serializable]
    public class OverrideFieldManager<TOverrideField> : OverrideFieldManager<TOverrideField>.IEdit, ICopyable<OverrideFieldManager<TOverrideField>> where TOverrideField : BaseOverrideField<TOverrideField>, new()
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 기본 생성자. 원본 필드와 재정의된 필드를 초기화합니다.
        /// </summary>
        public OverrideFieldManager()
        {
            OriginalField = new TOverrideField();
            OverriddenField = new TOverrideField();
        }



        /// <summary>
        /// 필드 값을 매개변수로 받는 생성자. 원본 필드와 재정의된 필드를 설정합니다.
        /// </summary>
        /// <param name="originalField">원본 필드</param>
        /// <param name="overriddenField">재정의된 필드</param>
        public OverrideFieldManager(TOverrideField originalField, TOverrideField overriddenField)
        {
            OriginalField = originalField;
            OverriddenField = overriddenField;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// IEdit 인터페이스는 필드 편집을 위한 속성을 정의합니다.
        /// </summary>
        public interface IEdit
        {
            OverrideFieldManager<TOverrideField> Current { get; }
            TOverrideField OriginalField { get; set; }
            TOverrideField OverriddenField { get; set; }
        }



        ///======================================================================================================================================================



        //? 원본 필드



        [SerializeField] protected TOverrideField OriginalField = null;

        TOverrideField IEdit.OriginalField { get => OriginalField; set => OriginalField = value; }

        /// <summary>
        /// 원본 필드를 반환합니다.
        /// </summary>
        public TOverrideField GetOriginalField => OriginalField;



        //? 재정의 필드



        [SerializeField] protected TOverrideField OverriddenField = null;

        TOverrideField IEdit.OverriddenField { get => OverriddenField; set => OverriddenField = value; }

        /// <summary>
        /// 재정의된 필드를 반환합니다.
        /// </summary>
        public TOverrideField GetOverriddenField => OverriddenField;



        //? 재정의 여부



        [SerializeField] public bool UseOverride;



        ///======================================================================================================================================================



        OverrideFieldManager<TOverrideField> IEdit.Current => this;



        ///======================================================================================================================================================



        /// <summary>
        /// 주어진 필드 타입에 따라 적절한 필드를 반환합니다.
        /// </summary>
        /// <returns>선택된 필드</returns>
        public TOverrideField GetField(EOverrideFieldType fieldType)
        {
            switch (fieldType)
            {
                case EOverrideFieldType.Original: return OriginalField;
                case EOverrideFieldType.Overridden: return OverriddenField;
                case EOverrideFieldType.Auto: return UseOverride ? OverriddenField : OriginalField;
                default: return null;
            }
        }



        public void Copy(OverrideFieldManager<TOverrideField> original)
        {
            // Original만 복사합니다.
            OriginalField = original.OriginalField;
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 커스텀 에디터를 사용하려면, [Serializable]를 붙여야 합니다.
    /// </summary>
    /// <typeparam name="TCurrent">상속받는 클래스 자체를 이곳에 넣어야 합니다.</typeparam>
    [Serializable]
    public abstract class BaseOverrideField<TCurrent> : ICopyable<TCurrent> where TCurrent : class, new()
    {
        /// <summary>
        /// 현재 인스턴스에 다른 인스턴스의 값을 복사하는 메서드입니다.
        /// </summary>
        /// <param name="original">복사할 원본 인스턴스</param>
        public abstract void Copy(TCurrent original);
    }



    ///======================================================================================================================================================
}
