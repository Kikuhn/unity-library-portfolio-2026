using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using SitraUtils;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;



//? 대개 값들의 집합, "콜렉션" 위주로 정리한 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    /// <summary>
    /// 타입(Type)과 관련된 컬렉션 유틸리티를 제공하는 정적 클래스입니다.
    /// </summary>
    public static class SU_Collection_Types
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 타입 검색 시 적용할 옵션을 지정하는 열거형입니다.
        /// </summary>
        public enum TypeSearch
        {
            /// <summary>
            /// 지정된 T를 상속(또는 구현)하되, 추상 클래스와 값 타입(Struct)·인터페이스를 모두 제외한
            /// <b>구체(Concrete) 클래스</b>만 검색합니다.
            /// </summary>
            ConcreteClasses,

            /// <summary>
            /// 지정된 T를 상속(또는 구현)하는 모든 클래스를 검색합니다.
            /// (추상 클래스 포함, 인터페이스 제외, 값 타입(Struct) 제외)
            /// </summary>
            IncludeAbstract,

            /// <summary>
            /// 지정된 T를 상속(또는 구현)하는 모든 타입을 검색하되,
            /// 값 타입(Struct)까지 포함합니다. (인터페이스는 제외)
            /// </summary>
            IncludeValueType,

            /// <summary>
            /// 지정된 T를 상속(또는 구현)하는 모든 타입을 검색합니다.
            /// (추상 클래스, 인터페이스, 값 타입 전부 포함)
            /// </summary>
            All
        }



        ///======================================================================================================================================================



        /// <summary>
        /// T를 상속(또는 구현)하는 타입들을 <b>모든 어셈블리</b>에서 찾아,
        /// <paramref name="option"/>에 따라 필터링하여 배열로 반환합니다.
        /// </summary>
        /// <param name="baseType">검색할 대상의 베이스 클래스 또는 인터페이스의 타입</typeparam>
        /// <param name="option">검색 옵션</param>
        /// <returns>조건에 맞는 <see cref="Type"/> 배열</returns>
        public static Type[] GetTypesAssignableTo(Type baseType, TypeSearch option)
        {
            //. 현재 AppDomain에 로드된 모든 어셈블리에서 타입을 전부 가져옴


            //? baseType을 상속(또는 구현)하는 모든 타입들 추출
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => SafeGetTypes(a))
                .Where(t => baseType.IsAssignableFrom(t));

            //. 옵션에 따른 필터링
            return FilterByOption(option, allTypes);
        }

        /// <summary>
        /// T를 상속(또는 구현)하는 타입들을 <b>모든 어셈블리</b>에서 찾아,
        /// <paramref name="option"/>에 따라 필터링하여 배열로 반환합니다.
        /// </summary>
        /// <typeparam name="T">검색할 대상의 베이스 클래스 또는 인터페이스</typeparam>
        /// <param name="option">검색 옵션</param>
        /// <returns>조건에 맞는 <see cref="Type"/> 배열</returns>
        public static Type[] GetTypesAssignableTo<T>(TypeSearch option)
        {
            return GetTypesAssignableTo(typeof(T), option);
        }



        /// <summary>
        /// T를 상속(또는 구현)하는 타입들을 <b>특정 Assembly</b>에서만 찾아,
        /// <paramref name="option"/>에 따라 필터링하여 배열로 반환합니다.
        /// </summary>
        /// <param name="baseType">검색할 대상의 베이스 클래스 또는 인터페이스의 타입</typeparam>
        /// <param name="option">검색 옵션</param>
        /// <param name="assembly">검색 대상 Assembly</param>
        /// <returns>조건에 맞는 <see cref="Type"/> 배열</returns>
        public static Type[] GetTypesAssignableTo(Type baseType, TypeSearch option, Assembly assembly)
        {
            //. 지정한 Assembly 안의 타입만 가져옴
            var allTypes = SafeGetTypes(assembly)
                .Where(t => baseType.IsAssignableFrom(t));

            //. 2) 옵션에 따른 필터링
            return FilterByOption(option, allTypes);
        }

        /// <summary>
        /// T를 상속(또는 구현)하는 타입들을 <b>특정 Assembly</b>에서만 찾아,
        /// <paramref name="option"/>에 따라 필터링하여 배열로 반환합니다.
        /// </summary>
        /// <typeparam name="T">검색할 대상의 베이스 클래스 또는 인터페이스</typeparam>
        /// <param name="option">검색 옵션</param>
        /// <param name="assembly">검색 대상 Assembly</param>
        /// <returns>조건에 맞는 <see cref="Type"/> 배열</returns>
        public static Type[] GetTypesAssignableTo<T>(TypeSearch option, Assembly assembly)
        {
            return GetTypesAssignableTo(typeof(T), option, assembly);
        }


        /// <summary>
        /// T를 상속(또는 구현)하는 타입들을 <b>여러 Assembly</b>에서 찾아,
        /// <paramref name="option"/>에 따라 필터링하여 배열로 반환합니다.
        /// </summary>
        /// <param name="baseType">검색할 대상의 베이스 클래스 또는 인터페이스의 타입</typeparam>
        /// <param name="option">검색 옵션</param>
        /// <param name="assemblies">검색 대상 Assembly 배열</param>
        /// <returns>조건에 맞는 <see cref="Type"/> 배열</returns>
        public static Type[] GetTypesAssignableTo(Type baseType, TypeSearch option, params Assembly[] assemblies)
        {
            //. 주어진 Assembly들에서만 타입을 가져옴
            //. assemblies가 비어있을 수 있으므로 체크해도 좋음
            var allTypes = assemblies
                .SelectMany(a => SafeGetTypes(a))
                .Where(t => baseType.IsAssignableFrom(t));

            //. 2) 옵션에 따른 필터링
            return FilterByOption(option, allTypes);
        }

        /// <summary>
        /// T를 상속(또는 구현)하는 타입들을 <b>여러 Assembly</b>에서 찾아,
        /// <paramref name="option"/>에 따라 필터링하여 배열로 반환합니다.
        /// </summary>
        /// <typeparam name="T">검색할 대상의 베이스 클래스 또는 인터페이스</typeparam>
        /// <param name="option">검색 옵션</param>
        /// <param name="assemblies">검색 대상 Assembly 배열</param>
        /// <returns>조건에 맞는 <see cref="Type"/> 배열</returns>
        public static Type[] GetTypesAssignableTo<T>(TypeSearch option, params Assembly[] assemblies)
        {
            return GetTypesAssignableTo(typeof(T), option, assemblies);
        }



        private static Type[] FilterByOption(TypeSearch option, IEnumerable<Type> types)
        {
            switch (option)
            {
                case TypeSearch.ConcreteClasses:
                //. 구체 클래스만 (추상, 값타입, 인터페이스 제외)
                return types
                    .Where(t => t.IsClass && !t.IsAbstract && !t.IsValueType)
                    .ToArray();

                case TypeSearch.IncludeAbstract:
                //. 클래스만 (추상 포함), 값타입·인터페이스 제외
                return types
                    .Where(t => t.IsClass && !t.IsValueType)
                    .ToArray();

                case TypeSearch.IncludeValueType:
                //. 클래스(추상 포함 가능) + struct(값타입) 허용, 인터페이스 제외
                //? "is interface"만 제외
                return types
                    .Where(t => !t.IsInterface)
                    .ToArray();

                case TypeSearch.All:
                default:
                //. 전부 허용
                return types.ToArray();
            }
        }



        /// <summary>
        /// 안전하게 <paramref name="assembly"/>의 모든 타입을 가져오는 메서드 예시
        /// (리플렉션 과정에서 예외가 발생할 수 있으므로 try-catch 등으로 처리)
        /// </summary>
        /// <param name="assembly">타입을 가져올 Assembly</param>
        /// <returns>Assembly 내의 모든 <see cref="Type"/> 배열</returns>
        private static Type[] SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch
            {
                //! 여기서 Assembly.GetTypes() 실패 시 예외 처리
                return new Type[0];
            }
        }



        /// <summary>
        /// 해당 타입를 상속(또는 구현)하는 타입들을 어셈블리 전역에서 찾아,
        /// <paramref name="option"/>에 따라 필터링하여 배열로 반환합니다.
        /// </summary>
        /// <param name="baseType">기준이 되는 베이스 타입 또는 인터페이스의 타입</typeparam>
        /// <param name="option">검색 옵션</param>
        /// <param name="result">조건에 맞는 <see cref="Type"/> 배열</param>
        public static void CreateTypesAssignableTo(Type baseType, TypeSearch option, out Type[] result)
        {
            result = GetTypesAssignableTo(baseType, option);
        }

        /// <summary>
        /// 제네릭 T를 상속(또는 구현)하는 타입들을 어셈블리 전역에서 찾아,
        /// <paramref name="option"/>에 따라 필터링하여 배열로 반환합니다.
        /// </summary>
        /// <typeparam name="T">기준이 되는 베이스 타입 또는 인터페이스</typeparam>
        /// <param name="option">검색 옵션</param>
        /// <param name="result">조건에 맞는 <see cref="Type"/> 배열</param>
        public static void CreateTypesAssignableTo<T>(TypeSearch option, out Type[] result)
        {
            result = GetTypesAssignableTo<T>(option);
        }



        /// <summary>
        /// 해당 타입을 상속(또는 구현)하는 타입들을 어셈블리 전역에서 찾아,
        /// <paramref name="option"/>에 따라 필터링하여 배열로 반환합니다.
        /// </summary>
        /// <param name="baseType">기준이 되는 베이스 타입 또는 인터페이스의 타입</typeparam>
        /// <param name="option">검색 옵션</param>
        /// /// <param name="assembly">검색 대상 Assembly</param>
        /// <param name="result">조건에 맞는 <see cref="Type"/> 배열</param>
        public static void CreateTypesAssignableTo(Type baseType, TypeSearch option, Assembly assembly, out Type[] result)
        {
            result = GetTypesAssignableTo(baseType, option, assembly);
        }

        /// <summary>
        /// 제네릭 T를 상속(또는 구현)하는 타입들을 어셈블리 전역에서 찾아,
        /// <paramref name="option"/>에 따라 필터링하여 배열로 반환합니다.
        /// </summary>
        /// <typeparam name="T">기준이 되는 베이스 타입 또는 인터페이스</typeparam>
        /// <param name="option">검색 옵션</param>
        /// /// <param name="assembly">검색 대상 Assembly</param>
        /// <param name="result">조건에 맞는 <see cref="Type"/> 배열</param>
        public static void CreateTypesAssignableTo<T>(TypeSearch option, Assembly assembly, out Type[] result)
        {
            result = GetTypesAssignableTo<T>(option, assembly);
        }


        /// <summary>
        /// 해당 타입을 상속(또는 구현)하는 타입들을 어셈블리 전역에서 찾아,
        /// <paramref name="option"/>에 따라 필터링하여 배열로 반환합니다.
        /// </summary>
        /// <param name="baseType">기준이 되는 베이스 타입 또는 인터페이스의 타입</typeparam>
        /// <param name="option">검색 옵션</param>
        /// <param name="assemblies">검색 대상 Assembly 배열</param>
        /// <param name="result">조건에 맞는 <see cref="Type"/> 배열</param>
        public static void CreateTypesAssignableTo(Type baseType, TypeSearch option, Assembly[] assemblies, out Type[] result)
        {
            result = GetTypesAssignableTo(baseType, option, assemblies);
        }

        /// <summary>
        /// 제네릭 T를 상속(또는 구현)하는 타입들을 어셈블리 전역에서 찾아,
        /// <paramref name="option"/>에 따라 필터링하여 배열로 반환합니다.
        /// </summary>
        /// <typeparam name="T">기준이 되는 베이스 타입 또는 인터페이스</typeparam>
        /// <param name="option">검색 옵션</param>
        /// <param name="assemblies">검색 대상 Assembly 배열</param>
        /// <param name="result">조건에 맞는 <see cref="Type"/> 배열</param>
        public static void CreateTypesAssignableTo<T>(TypeSearch option, Assembly[] assemblies, out Type[] result)
        {
            result = GetTypesAssignableTo<T>(option, assemblies);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 검색 옵션(<paramref name="option"/>)에 따라 찾은 타입들의 Name(또는 FullName)을 문자열 배열로 만들어 반환합니다.
        /// </summary>
        /// <typeparam name="T">기준이 되는 베이스 타입 또는 인터페이스</typeparam>
        /// <param name="option">검색 옵션</param>
        /// <param name="useFullName">true면 FullName, false면 Name 사용</param>
        /// <param name="names">결과 문자열 배열</param>
        public static void CreateTypeNameArray<T>(TypeSearch option, bool useFullName, out string[] names)
        {
            var types = GetTypesAssignableTo<T>(option);
            names = new string[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                names[i] = useFullName ? types[i].FullName : types[i].Name;
            }
        }



        /// <summary>
        /// 검색 옵션(<paramref name="option"/>)에 따라 찾은 타입들 각각에 대해
        /// Activator.CreateInstance()로 객체를 생성하고, 배열로 묶어 반환합니다.
        /// </summary>
        /// <typeparam name="T">베이스 타입(클래스). Activator 생성이 가능해야 함.</typeparam>
        /// <param name="option">검색 옵션</param>
        /// <param name="instances">생성된 객체 배열</param>
        public static void CreateTypeArray<T>(TypeSearch option, out T[] instances) where T : class
        {
            var types = GetTypesAssignableTo<T>(option);
            instances = new T[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                instances[i] = Activator.CreateInstance(types[i]) as T;
                //instances[i] = CreateInstanceByExpression<T>(types[i]);
            }
        }



        /// <summary>
        /// 검색 옵션(<paramref name="option"/>)에 따라 찾은 타입들 각각에 대해
        /// Activator.CreateInstance()로 객체를 생성해, Dictionary(Type, T)을 만든 뒤 반환합니다.
        /// </summary>
        /// <typeparam name="T">베이스 타입(클래스). Activator 생성이 가능해야 함.</typeparam>
        /// <param name="option">검색 옵션</param>
        /// <param name="targetDic">생성된 Dictionary 결과</param>
        /// <param name="afterEvent">객체 생성 직후에 실행할 액션(옵션)</param>
        public static void CreateTypeDictionary_Legacy<T>(TypeSearch option, out Dictionary<Type, T> targetDic, Action<T> afterEvent = null) where T : class
        {
            var types = GetTypesAssignableTo<T>(option);
            targetDic = new Dictionary<Type, T>(types.Length);

            for (int i = 0; i < types.Length; i++)
            {
                T instance = Activator.CreateInstance(types[i]) as T;
                afterEvent?.Invoke(instance);
                targetDic.Add(types[i], instance);
            }
        }



        /// <summary>
        /// 검색 옵션(<paramref name="option"/>)에 따라 찾은 타입들 각각에 대해
        /// Activator.CreateInstance()로 객체를 생성해, Dictionary(Type, T)을 만든 뒤 반환합니다.
        /// </summary>
        /// <typeparam name="T">베이스 타입(클래스). Activator 생성이 가능해야 함.</typeparam>
        /// <param name="option">검색 옵션</param>
        /// <param name="targetDic">생성된 Dictionary 결과</param>
        /// <param name="afterEvent">객체 생성 직후에 실행할 액션(옵션)</param>
        public static void CreateTypeDictionary<T>(
            TypeSearch option,
            out Dictionary<Type, T> targetDic,
            Action<T> afterEvent = null
            ) where T : class
        {
            //. 1) T를 상속(또는 구현)하는 모든 타입(Concrete 등 옵션 필터)을 가져옴
            var types = GetTypesAssignableTo<T>(option);

            //. 2) Dictionary 초기화
            targetDic = new Dictionary<Type, T>(types.Length);

            //. 3) 타입별로 '파라미터 없는 생성자' 생성 Delegate를 생성/캐싱
            foreach (var type in types)
            {
                //. Expression 통해 만든 Delegate (미리 static Dictionary에 캐싱해도 됨)
                //T instance = CreateInstanceByExpression<T>(type);
                T instance = Activator.CreateInstance(type) as T;
                afterEvent?.Invoke(instance);
                targetDic.Add(type, instance);
            }
        }



        //. Expression Tree로 Activator.CreateInstance 대신 호출
        private static T CreateInstanceByExpression<T>(Type type) where T : class
        {
            //. 여기에, 이미 캐싱된 delegate가 있으면 캐싱 재사용. (생략 가능)

            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
            {
                throw new InvalidOperationException($"{type}에 파라미터 없는 생성자가 없습니다.");
            }

            //. "new {type}()" 식 Expression 생성
            var newExp = System.Linq.Expressions.Expression.New(ctor);
            //. Func<object> 형태라면 아래:
            // var lambdaObj = Expression.Lambda<Func<object>>(newExp).Compile();
            // return (T)lambdaObj();
            //? 또는 Func<T>로 바로 캐스팅이 되도록 표현
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<T>>(newExp);
            var creator = lambda.Compile();

            return creator();
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 지정된 <paramref name="type"/>이 내부(중첩) 클래스일 경우,
        /// 가장 바깥 클래스부터 바로 직전 중첩 클래스까지의 전체 경로를
        /// 점(.)으로 연결된 문자열 배열로 반환합니다.
        /// 예를 들어 <c>SkelSbject_PixelHuman.ASkin.Skins</c> 라는 클래스가 있다면
        /// <c>["SkelSbject_PixelHuman", "SkelSbject_PixelHuman.ASkin"]</c> 형태로 반환합니다.
        /// </summary>
        /// <param name="type">중첩 클래스를 검사하고자 하는 대상 <see cref="Type"/>.</param>
        /// <returns>
        /// 내부(중첩) 클래스라면, 바깥 클래스부터 자신 직전까지의 경로를 점(.)으로 연결한 문자열의 배열을 반환합니다.
        /// 중첩 클래스가 아니거나(최상위 타입인 경우) 매개변수가 <see langword="null"/>이면 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/>이 <see langword="null"/>인 경우 발생합니다.</exception>
        /// <remarks>
        /// <para>
        /// 예시: 
        /// <c>SkelSbject_PixelHuman.ASkin.Skins</c> 타입일 경우 반환: 
        /// <c>["SkelSbject_PixelHuman", "SkelSbject_PixelHuman.ASkin"]</c>.
        /// </para>
        /// <para>
        /// 만약 <paramref name="type"/>이 <c>SkelSbject_PixelHuman</c>처럼 최상위 타입이라면 
        /// 내부 클래스가 아니므로 <see langword="null"/>을 반환합니다.
        /// </para>
        /// </remarks>
        public static string[] GetNestedClassChainExceptLast(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), "검사할 Type이 null입니다.");

            // 내부(중첩) 클래스가 아니라면(=DeclaringType이 없다면) null 반환
            if (type.DeclaringType == null)
                return null;

            // 바깥 → 안쪽 순으로 타입을 나열하기 위해 가장 안쪽(=매개변수 type)부터 
            // DeclaringType을 따라 밖으로 이동하며 수집
            List<Type> chain = new List<Type>();
            Type current = type;
            while (current != null)
            {
                chain.Add(current);
                current = current.DeclaringType;
            }

            // 현재 chain 순서: [가장 안쪽, 그 바깥, ...] 이므로 Reverse를 호출해서
            // [가장 바깥, 중간, ..., 가장 안쪽] 순서가 되도록 뒤집는다
            chain.Reverse();

            // (가장 바깥) → (중간) → (제일 안쪽) 으로 이동하면서,
            // "누적된 점(.) 연결 이름"들을 담을 리스트
            List<string> partialPaths = new List<string>();

            string accumulatedName = chain[0].Name;  // 가장 바깥 클래스 이름
            partialPaths.Add(accumulatedName);

            // 1개 이상의 중첩 단계가 있을 경우, 
            // 계속 점(.)을 붙여가며 누적된 경로 문자열을 만든다.
            for (int i = 1; i < chain.Count; i++)
            {
                accumulatedName += "." + chain[i].Name;
                partialPaths.Add(accumulatedName);
            }

            // 예: SkelSbject_PixelHuman.ASkin.Skins -> partialPaths =
            // ["SkelSbject_PixelHuman",
            //  "SkelSbject_PixelHuman.ASkin",
            //  "SkelSbject_PixelHuman.ASkin.Skins"]

            // "가장 안쪽(= 자기 자신)"은 제외해야 하므로 마지막 요소 제거
            if (partialPaths.Count > 0)
            {
                partialPaths.RemoveAt(partialPaths.Count - 1);
            }

            // 만약 이미 한 단계만 있었던 경우(중첩 클래스가 아닌 최상위 클래스)라면
            // 위에서 null을 반환했으므로 여기선 그 상황이 발생하지 않는다.
            // 남아 있는 것이 있다면 그대로 string[] 변환하여 반환
            return (partialPaths.Count > 0) ? partialPaths.ToArray() : null;
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 열거형(Enum)과 관련된 컬렉션 유틸리티를 제공하는 정적 클래스입니다.
    /// <para>주로 Enum → 배열, Enum → Dictionary 변환 등을 돕습니다.</para>
    /// </summary>
    public static class SU_Collection_Enums
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 제네릭 열거형 <typeparamref name="TEnum"/>의 모든 값을 배열로 반환합니다.
        /// </summary>
        /// <typeparam name="TEnum">Enum 타입</typeparam>
        /// <returns><paramref name="TEnum"/>에 정의된 모든 Enum 멤버를 담은 배열</returns>
        public static TEnum[] GetEnumArray<TEnum>() where TEnum : Enum
        {
            return Enum.GetValues(typeof(TEnum)) as TEnum[];
        }



        /// <summary>
        /// 제네릭 열거형 <typeparamref name="TEnum"/>의 모든 값을 배열로 반환합니다.
        /// (out 파라미터 버전)
        /// </summary>
        /// <typeparam name="TEnum">Enum 타입</typeparam>
        /// <param name="target">함수 내에서 생성된 <paramref name="TEnum"/> 배열</param>
        public static void SetEnumArray<TEnum>(out TEnum[] target) where TEnum : Enum
        {
            target = GetEnumArray<TEnum>();
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 제네릭 열거형 <typeparamref name="TEnum"/>의 모든 멤버를 Key로, 
        /// 제네릭 클래스 <typeparamref name="TClass"/> (new() 가능)을 Value로 하는 Dictionary를 생성합니다.
        /// </summary>
        /// <typeparam name="TEnum">Enum 타입 (struct, Enum, IComparable, IConvertible, IFormattable)</typeparam>
        /// <typeparam name="TClass">클래스 타입. new()를 통해 인스턴스 생성 가능</typeparam>
        /// <param name="dictionary">생성된 Dictionary (Key: <typeparamref name="TEnum"/>, Value: <typeparamref name="TClass"/>)</param>
        public static void SetEnumClassDictionary<TEnum, TClass>(out Dictionary<TEnum, TClass> dictionary)
            where TEnum : struct, Enum, IComparable, IConvertible, IFormattable
            where TClass : class, new()
        {
            SetEnumArray<TEnum>(out var enums);
            dictionary = new Dictionary<TEnum, TClass>(enums.Length);

            for (int i = 0; i < enums.Length; i++)
            {
                dictionary.Add(enums[i], new TClass());
            }
        }



        /// <summary>
        /// 제네릭 열거형 <typeparamref name="TEnumKey"/>의 모든 멤버를 Key로 하는 
        /// Dictionary(Key: <typeparamref name="TEnumKey"/>, Value: <typeparamref name="TValue"/>)를 생성합니다.
        /// <para>Value는 초기화되지 않습니다.</para>
        /// </summary>
        /// <typeparam name="TEnumKey">Enum 타입 (struct, Enum, IComparable, IConvertible, IFormattable)</typeparam>
        /// <typeparam name="TValue">Dictionary의 Value 타입</typeparam>
        /// <param name="dictionary">생성된 Dictionary</param>
        public static void SetEnumDictionary<TEnumKey, TValue>(out Dictionary<TEnumKey, TValue> dictionary)
            where TEnumKey : struct, Enum, IComparable, IConvertible, IFormattable
        {
            dictionary = new Dictionary<TEnumKey, TValue>(EnumComparer.For<TEnumKey>());
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 제네릭 열거형 <typeparamref name="TEnumKey"/>의 모든 멤버를 Key로 하는 
        /// Dictionary(Key: <typeparamref name="TEnumKey"/>, Value: <typeparamref name="TValue"/>)를 생성합니다.
        /// <para>Value는 <typeparamref name="TValue"/>의 new()를 통해 자동 생성됩니다.</para>
        /// </summary>
        /// <typeparam name="TEnumKey">Enum 타입 (struct, Enum, IComparable, IConvertible, IFormattable)</typeparam>
        /// <typeparam name="TValue">클래스나 struct. new() 가능</typeparam>
        /// <param name="dictionary">생성된 Dictionary</param>
        public static void SetEnumDictionaryNew<TEnumKey, TValue>(out Dictionary<TEnumKey, TValue> dictionary)
            where TEnumKey : struct, Enum, IComparable, IConvertible, IFormattable
            where TValue : new()
        {
            dictionary = new Dictionary<TEnumKey, TValue>(EnumComparer.For<TEnumKey>());
            foreach (var v in GetEnumArray<TEnumKey>())
            {
                dictionary.Add(v, new TValue());
            }
        }



        /// <summary>
        /// 제네릭 열거형 <typeparamref name="TEnumKey"/>를 Key로 하고 
        /// <typeparamref name="TValue"/>를 Value로 하는 Dictionary를 생성(초기화)하여 반환합니다.
        /// <para>Value는 초기화되지 않습니다.</para>
        /// </summary>
        /// <typeparam name="TEnumKey">Enum 타입</typeparam>
        /// <typeparam name="TValue">Dictionary의 Value 타입</typeparam>
        /// <returns>생성된 Dictionary (Key: <typeparamref name="TEnumKey"/>, Value: <typeparamref name="TValue"/>)</returns>
        public static Dictionary<TEnumKey, TValue> NewEnumDictionary<TEnumKey, TValue>()
            where TEnumKey : struct, Enum, IComparable, IConvertible, IFormattable
        {
            SetEnumDictionary<TEnumKey, TValue>(out var resultDictionary);
            return resultDictionary;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 제네릭 열거형 <typeparamref name="TEnumKey"/>의 모든 멤버를 Key로,
        /// <c>string</c>을 Value로 하는 Dictionary를 생성하며, 
        /// 배열(<paramref name="keyArray"/>)로도 함께 반환합니다.
        /// <para>Value에는 해당 enum 멤버의 <c>ToString()</c>이 들어갑니다.</para>
        /// </summary>
        /// <typeparam name="TEnumKey">Enum 타입</typeparam>
        /// <param name="keyArray">생성된 Enum 멤버 배열</param>
        /// <param name="dictionary">Key: enum 멤버, Value: enum.ToString() 문자열</param>
        public static void SetEnumStringDictionary<TEnumKey>(out TEnumKey[] keyArray, out Dictionary<TEnumKey, string> dictionary)
            where TEnumKey : struct, Enum, IComparable, IConvertible, IFormattable
        {
            SetEnumArray(out keyArray);
            dictionary = new Dictionary<TEnumKey, string>(keyArray.Length, EnumComparer.For<TEnumKey>());
            for (int i = 0; i < keyArray.Length; i++)
            {
                dictionary.Add(keyArray[i], keyArray[i].ToString());
            }
        }



        /// <summary>
        /// 제네릭 열거형 <typeparamref name="TEnumKey"/>의 모든 멤버를 Key로,
        /// <c>string</c>을 Value로 하는 Dictionary를 생성합니다.
        /// <para>Value에는 해당 enum 멤버의 <c>ToString()</c>이 들어갑니다.</para>
        /// </summary>
        /// <typeparam name="TEnumKey">Enum 타입</typeparam>
        /// <param name="dictionary">생성된 Dictionary (Key: enum 멤버, Value: 문자열)</param>
        public static void SetEnumStringDictionary<TEnumKey>(out Dictionary<TEnumKey, string> dictionary)
            where TEnumKey : struct, Enum, IComparable, IConvertible, IFormattable
        {
            SetEnumStringDictionary(out var n, out dictionary);
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 일반적인 컬렉션(리스트, 해시셋 등)에 대한 유틸리티 메서드를 제공하는 정적 클래스입니다.
    /// </summary>
    public static class SU_Collection
    {
        ///======================================================================================================================================================



        /// <summary>
        /// <paramref name="targetList"/>의 모든 요소를 추가(Copy)합니다.
        /// <para>이 리스트의 Capacity가 현재 리스트.Count + <paramref name="targetList"/>.Count로 설정된 후, 요소가 복사됩니다.</para>
        /// </summary>
        /// <typeparam name="T">리스트 요소의 타입</typeparam>
        /// <param name="var">원본 리스트(추가 대상)</param>
        /// <param name="targetList">복사할 요소들을 보유한 리스트</param>
        public static void AppendAll<T>(this List<T> var, IList<T> targetList)
        {
            var.Capacity = var.Count + targetList.Count;
            for (int i = 0; i < targetList.Count; i++)
            {
                var.Add(targetList[i]);
            }
        }



        /// <summary>
        /// <paramref name="targetList"/>의 모든 요소를 <paramref name="var"/>에 추가(Copy)합니다.
        /// <para><paramref name="beforeClear"/>가 true면, <paramref name="var"/>는 먼저 Clear된 후 복사됩니다.</para>
        /// </summary>
        /// <typeparam name="T">리스트 요소의 타입</typeparam>
        /// <param name="var">원본 리스트(추가 대상)</param>
        /// <param name="targetList">복사할 요소들을 보유한 리스트</param>
        /// <param name="beforeClear">true면 <paramref name="var"/>를 먼저 Clear</param>
        public static void ReplaceAll<T>(this List<T> var, IList<T> targetList)
        {
            var.Capacity = targetList.Count;
            for (int i = 0; i < targetList.Count; i++)
            {
                var.Add(targetList[i]);
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 콜렉션(<paramref name="var"/>)에 <paramref name="item"/>을 추가한 뒤, 그 아이템을 반환합니다.
        /// <para>체이닝 용도로 사용할 수 있습니다.</para>
        /// </summary>
        /// <typeparam name="T">콜렉션 요소의 타입</typeparam>
        /// <param name="var">추가 대상 콜렉션</param>
        /// <param name="item">추가할 요소</param>
        /// <returns>추가된 요소(<paramref name="item"/>) 자체</returns>
        public static T AddReturn<T>(this ICollection<T> var, T item)
        {
            var.Add(item);
            return item;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 리스트(<paramref name="list"/>)에서 <paramref name="match"/> 조건을 만족하는 모든 요소들의 인덱스를 새로운 <see cref="List{T}"/>로 반환합니다.
        /// </summary>
        /// <typeparam name="T">리스트 요소의 타입</typeparam>
        /// <param name="list">검색할 리스트</param>
        /// <param name="match">조건을 정의하는 <see cref="Predicate{T}"/></param>
        /// <returns>조건을 만족하는 요소들의 인덱스 리스트</returns>
        /// <exception cref="ArgumentNullException">list 또는 match가 null일 경우</exception>
        public static List<int> FindAllIndices<T>(this IList<T> list, Predicate<T> match)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            List<int> indices = new List<int>();
            list.AppendMatchingIndices(match, indices);
            return indices;
        }



        /// <summary>
        /// 리스트(<paramref name="list"/>)에서 <paramref name="match"/> 조건을 만족하는 모든 요소들의 인덱스를
        /// <paramref name="indices"/> 리스트에 추가합니다.
        /// </summary>
        /// <typeparam name="T">리스트 요소의 타입</typeparam>
        /// <param name="list">검색할 리스트</param>
        /// <param name="match">조건을 정의하는 <see cref="Predicate{T}"/></param>
        /// <param name="indices">조건을 만족하는 인덱스를 담을 리스트 (기존 내용 뒤에 추가)</param>
        /// <exception cref="ArgumentNullException">list, match, indices 중 하나라도 null일 경우</exception>
        public static void AppendMatchingIndices<T>(this IList<T> list, Predicate<T> match, in List<int> indices)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }
            if (indices == null)
            {
                throw new ArgumentNullException(nameof(indices));
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (match(list[i]))
                {
                    indices.Add(i);
                }
            }
        }



        /// <summary>
        /// 리스트(<paramref name="list"/>)에서 <paramref name="match"/> 조건을 만족하는 요소들의 총 개수를 반환합니다.
        /// </summary>
        /// <typeparam name="T">리스트 요소의 타입</typeparam>
        /// <param name="list">검색할 리스트</param>
        /// <param name="match">조건을 정의하는 <see cref="Predicate{T}"/></param>
        /// <returns>조건을 만족하는 요소들의 개수</returns>
        /// <exception cref="ArgumentNullException">list 또는 match가 null일 경우</exception>
        public static int CountWhere<T>(this IList<T> list, Predicate<T> match)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (match(list[i]))
                {
                    count++;
                }
            }
            return count;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 주어진 조건에 맞는 모든 요소를 리스트에서 찾아 반환하고, 해당 요소들은 리스트에서 제거합니다.
        /// </summary>
        /// <typeparam name="T">리스트 요소의 타입</typeparam>
        /// <param name="list">대상 리스트</param>
        /// <param name="match">요소를 필터링할 조건</param>
        /// <returns>조건을 만족한 요소들의 리스트</returns>
        public static List<T> FindAndRemoveAll<T>(this List<T> list, Predicate<T> match)
        {
            //? 조건에 맞는 요소들을 따로 리스트로 수집
            var matched = list.FindAll(match);

            //! 요소가 존재한다면 리스트에서 제거
            if (matched.Count > 0)
            {
                list.RemoveAll(match);
            }

            return matched;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// Stack에 여러 개의 요소를 한 번에 Push합니다.
        /// </summary>
        public static void PushRange<T>(this Stack<T> stack, IEnumerable<T> items)
        {
            if (stack == null) throw new ArgumentNullException(nameof(stack));
            if (items == null) throw new ArgumentNullException(nameof(items));

            // 1) IList<T>이면 인덱스로 직접 Push
            if (items is IList<T> list)
            {
                for (int i = 0, n = list.Count; i < n; i++)
                {
                    stack.Push(list[i]);
                }
            }
            else
            {
                // 2) 그 외에는 foreach로 한 번만 순회
                foreach (var item in items)
                {
                    stack.Push(item);
                }
            }
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    //? 인스턴스 팩토리 매니저



    /// <summary>
    /// 파라미터 없는 기본 생성자를 가진 <typeparamref name="TBase"/> (또는 파생 타입)의
    /// 인스턴스를 빠르게 생성·캐싱해 주는 매니저입니다.
    /// <para>
    /// • **Mono/JIT** : <c>DynamicMethod</c>로 <c>newobj</c> IL을 방출해 <c>new</c> 수준의 성능 제공<br/>
    /// • **IL2CPP/iOS/WebGL(AOT)** : <see cref="Activator.CreateInstance(Type,bool)"/> 경로로 자동 폴백
    /// </para>
    /// </summary>
    /// <typeparam name="TBase">기준이 되는 레퍼런스 타입</typeparam>
    public sealed class InstanceFactoryManager<TBase> : IDisposable
        where TBase : class
    {
        /// <summary>타입별 생성 델리게이트 캐시 (스레드 안전)</summary>
        private readonly ConcurrentDictionary<Type, Func<TBase>> _cache = new();

        private int _disposed; //. 0 = alive, 1 = disposed



        //================================================================================================================
        //  Public API
        //================================================================================================================

        /// <summary>
        /// 제네릭 인자 <typeparamref name="TDerived"/> 인스턴스를 생성합니다.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDerived CreateInstance<TDerived>() where TDerived : class, TBase
            => (TDerived)CreateInstance(typeof(TDerived));

        /// <summary>
        /// 런타임에 전달된 <paramref name="type"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <exception cref="ObjectDisposedException">매니저가 이미 해제된 경우</exception>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> 이 <see langword="null"/></exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="type"/> 이 <typeparamref name="TBase"/> 와 호환되지 않거나 파라미터 없는 생성자가 없을 때
        /// </exception>
        public TBase CreateInstance(Type type)
        {
            ThrowIfDisposed();

            if (type is null)
                throw new ArgumentNullException(nameof(type)); //! 인자 검사

            if (!typeof(TBase).IsAssignableFrom(type))
                throw new ArgumentException($"{type.FullName} 은(는) {typeof(TBase).FullName} 과 호환되지 않습니다.", nameof(type));

            //. 타입별 델리게이트 캐싱 후 호출
            var factory = _cache.GetOrAdd(type, BuildFactory);
            return factory();
        }

        /// <summary>모든 캐시 엔트리를 제거합니다. (DynamicMethod 자체는 CLR 레벨에서 해제되지 않습니다)</summary>
        public void ClearCache() => _cache.Clear();



        //================================================================================================================
        //  IDisposable
        //================================================================================================================

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return; //. 재진입 방지
            ClearCache();
            GC.SuppressFinalize(this);
        }



        //================================================================================================================
        //  Internal Helpers
        //================================================================================================================

        /// <summary>
        /// 타입별 생성 델리게이트를 빌드합니다. (플랫폼별 전처리기 분기)
        /// </summary>
        private static Func<TBase> BuildFactory(Type type)
        {
#if ENABLE_IL2CPP
            //. IL2CPP : Reflection.Emit 미지원 → Activator 경로
            return () => (TBase)Activator.CreateInstance(type, true)!; //! 비공개 기본 생성자 허용
#else
            //. Mono/JIT : Reflection.Emit.DynamicMethod 사용
            var ctor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null, types: Type.EmptyTypes, modifiers: null)
                ?? throw new ArgumentException($"{type.FullName} 에 파라미터 없는 생성자가 없습니다.", nameof(type));

            var dm = new System.Reflection.Emit.DynamicMethod(
                name: $"_IFM_ctor_{type.FullName}",
                returnType: typeof(TBase),
                parameterTypes: Type.EmptyTypes,
                m: typeof(InstanceFactoryManager<TBase>).Module,
                skipVisibility: true);

            var il = dm.GetILGenerator();
            il.Emit(System.Reflection.Emit.OpCodes.Newobj, ctor); //. newobj <.ctor>
            il.Emit(System.Reflection.Emit.OpCodes.Ret);

            return (Func<TBase>)dm.CreateDelegate(typeof(Func<TBase>));
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(InstanceFactoryManager<TBase>));
        }
    }



    ///======================================================================================================================================================



    //? 열거형 확장



    /// <summary>
    /// Enum 타입을 관리하는 매니저 클래스입니다.
    /// 지정된 Enum 타입을 추가, 제거, 확인할 수 있습니다.
    /// </summary>
    /// <typeparam name="Ttype">Enum 타입입니다.</typeparam>
    [Serializable]
    public class EnumManager<Ttype>
        where Ttype : struct, Enum, IComparable, IConvertible, IFormattable
    {
        /// <summary>
        /// 기본 생성자로, 관리할 Enum 타입을 초기화합니다.
        /// </summary>
        public EnumManager(int capacity = 0)
        {
            Types = new HashSet<Ttype>(capacity);
        }



        /// <summary>
        /// 관리할 Enum 타입의 컬렉션입니다.
        /// </summary>
        private readonly HashSet<Ttype> Types;



        /// <summary>
        /// 지정된 Enum 값을 추가합니다.
        /// </summary>
        /// <param name="value">추가할 Enum 값입니다.</param>
        public void Set(Ttype value)
        {
            Types.Add(value);
        }



        /// <summary>
        /// 여러 Enum 값을 추가합니다.
        /// </summary>
        /// <param name="value">추가할 Enum 값 배열입니다.</param>
        public void Sets(params Ttype[] value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                Types.Add(value[i]);
            }
        }



        /// <summary>
        /// 지정된 Enum 값이 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="value">확인할 Enum 값입니다.</param>
        /// <returns>포함되어 있으면 true를 반환합니다.</returns>
        public bool Check(Ttype value)
        {
            return Types.Contains(value);
        }



        /// <summary>
        /// 여러 Enum 값이 모두 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="value">확인할 Enum 값 배열입니다.</param>
        /// <returns>모두 포함되어 있으면 true를 반환합니다.</returns>
        public bool Checks(params Ttype[] value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (!Types.Contains(value[i]))
                {
                    return false;
                }
            }
            return true;
        }



        /// <summary>
        /// 지정된 Enum 값을 제거합니다.
        /// </summary>
        /// <param name="value">제거할 Enum 값입니다.</param>
        public void Remove(Ttype value)
        {
            Types.Remove(value);
        }



        /// <summary>
        /// 모든 Enum 값을 제거합니다.
        /// </summary>
        public void Clear()
        {
            Types.Clear();
        }
    }



    /// <summary>
    /// Enum 키와 연관된 리스트를 관리하는 클래스입니다.
    /// </summary>
    /// <typeparam name="TEnum">Enum 키 타입입니다.</typeparam>
    /// <typeparam name="T">리스트에 포함될 데이터 타입입니다.</typeparam>
    [Serializable]
    public class EnumList<TEnum, T>
        where TEnum : struct, Enum, IComparable, IConvertible, IFormattable
    {
        /// <summary>
        /// 생성자로, Enum 키와 연관된 빈 리스트를 초기화합니다.
        /// </summary>
        /// <param name="capacity">리스트의 초기 용량입니다. 0이면 기본값을 사용합니다.</param>
        public EnumList(int capacity = 0)
        {
            SU_Collection_Enums.SetEnumDictionaryNew(out Lists);

            if (capacity != 0)
            {
                foreach (var v in Lists)
                {
                    v.Value.Capacity = capacity;
                }
            }
        }



        /// <summary>
        /// Enum 키와 연관된 리스트의 사전입니다.
        /// </summary>
        private readonly Dictionary<TEnum, List<T>> Lists;



        /// <summary>
        /// 지정된 Enum 키와 연관된 리스트를 반환합니다.
        /// </summary>
        /// <param name="type">리스트를 가져올 Enum 키입니다.</param>
        /// <returns>해당 Enum 키와 연관된 리스트입니다.</returns>
        public List<T> GetList(TEnum type)
        {
            return Lists[type];
        }
    }



    ///======================================================================================================================================================



    //? 딕셔너리 확장



    /// <summary>
    /// 하나의 키-값 쌍을 양방향으로 저장하는 DoubleDictionary입니다.
    /// <para>예: A타입 키와 B타입 값으로 저장 시, 반대로 B타입 키에 대해서도 A타입 값을 빠르게 조회할 수 있습니다.</para>
    /// </summary>
    /// <typeparam name="TKeyA">Dictionary A의 키 타입</typeparam>
    /// <typeparam name="TKeyB">Dictionary B의 키 타입</typeparam>
    [Serializable]
    public class DoubleDictionary<TKeyA, TKeyB>
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 지정된 초기 용량(<paramref name="capacity"/>)으로 내부 딕셔너리를 생성합니다.
        /// </summary>
        /// <param name="capacity">초기 용량</param>
        public DoubleDictionary(int capacity)
        {
            Dictionary_A = new Dictionary<TKeyA, TKeyB>(capacity);
            Dictionary_B = new Dictionary<TKeyB, TKeyA>(capacity);
        }

        /// <summary>
        /// 이미 존재하는 두 Dictionary를 받아서 초기화할 수 있습니다.
        /// <para>이때, Dictionary A와 B는 서로 짝이 맞도록 구성되어 있어야 합니다.</para>
        /// </summary>
        /// <param name="dictionary_A">A→B 맵핑을 담은 Dictionary</param>
        /// <param name="dictionary_B">B→A 맵핑을 담은 Dictionary</param>
        public DoubleDictionary(Dictionary<TKeyA, TKeyB> dictionary_A, Dictionary<TKeyB, TKeyA> dictionary_B)
        {
            Dictionary_A = dictionary_A;
            Dictionary_B = dictionary_B;
        }




        ///======================================================================================================================================================



        /// <summary>
        /// A→B 맵핑을 보관하는 내부 딕셔너리
        /// </summary>
        private readonly Dictionary<TKeyA, TKeyB> Dictionary_A;

        /// <summary>
        /// B→A 맵핑을 보관하는 내부 딕셔너리
        /// </summary>
        private readonly Dictionary<TKeyB, TKeyA> Dictionary_B;



        /// <summary>
        /// A→B 맵핑 딕셔너리를 읽기 전용으로 접근할 수 있는 프로퍼티
        /// </summary>
        public IReadOnlyDictionary<TKeyA, TKeyB> GetDictionary_A => Dictionary_A;

        /// <summary>
        /// B→A 맵핑 딕셔너리를 읽기 전용으로 접근할 수 있는 프로퍼티
        /// </summary>
        public IReadOnlyDictionary<TKeyB, TKeyA> GetDictionary_B => Dictionary_B;



        public TKeyB this[TKeyA keyA]
        {
            get
            {
                return Dictionary_A[keyA];

            }
            set
            {
                Dictionary_A[keyA] = value;
            }
        }



        public TKeyA this[TKeyB keyB]
        {
            get
            {
                return Dictionary_B[keyB];
            }
            set
            {
                Dictionary_B[keyB] = value;
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// A타입 키와 B타입 값을 함께 추가합니다.
        /// <para>내부적으로 Dictionary_A와 Dictionary_B에 동시에 등록되며, 
        /// 동일한 키가 이미 존재하면 예외가 발생할 수 있습니다.</para>
        /// </summary>
        /// <param name="keyA">A타입 키</param>
        /// <param name="keyB">B타입 값(동시에 B타입 키)</param>
        public void Add(TKeyA keyA, TKeyB keyB)
        {
            Dictionary_A.Add(keyA, keyB);
            Dictionary_B.Add(keyB, keyA);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// A타입 키(<paramref name="keyA"/>)로 항목을 제거합니다.
        /// <para>해당 키가 존재하지 않으면 <see cref="KeyNotFoundException"/>이 발생할 수 있습니다.</para>
        /// </summary>
        /// <param name="keyA">제거할 A타입 키</param>
        public void Remove_A(TKeyA keyA)
        {
            // Dictionary_A[keyA]를 통해 B값을 찾은 뒤, B키로도 제거
            Dictionary_B.Remove(Dictionary_A[keyA]);
            Dictionary_A.Remove(keyA);
        }

        /// <summary>
        /// B타입 키(<paramref name="keyB"/>)로 항목을 제거합니다.
        /// <para>해당 키가 존재하지 않으면 <see cref="KeyNotFoundException"/>이 발생할 수 있습니다.</para>
        /// </summary>
        /// <param name="keyB">제거할 B타입 키</param>
        public void Remove_B(TKeyB keyB)
        {
            // Dictionary_B[keyB]를 통해 A값을 찾은 뒤, A키로도 제거
            Dictionary_A.Remove(Dictionary_B[keyB]);
            Dictionary_B.Remove(keyB);
        }

        /// <summary>
        /// A타입 키와 B타입 키가 동시에 지정되었을 때, 그 쌍을 제거합니다.
        /// </summary>
        /// <param name="keyA">A타입 키</param>
        /// <param name="keyB">B타입 키</param>
        public void Remove(TKeyA keyA, TKeyB keyB)
        {
            Dictionary_A.Remove(keyA);
            Dictionary_B.Remove(keyB);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// A타입 키(<paramref name="keyA"/>)로부터 B타입 값을 조회합니다.
        /// </summary>
        /// <param name="keyA">찾을 A타입 키</param>
        /// <returns>해당 A타입 키에 대응하는 B타입 값</returns>
        /// <exception cref="KeyNotFoundException">키가 존재하지 않을 때 발생</exception>
        public TKeyB GetValue_A(TKeyA keyA)
        {
            return Dictionary_A[keyA];
        }

        /// <summary>
        /// B타입 키(<paramref name="keyB"/>)로부터 A타입 값을 조회합니다.
        /// </summary>
        /// <param name="keyB">찾을 B타입 키</param>
        /// <returns>해당 B타입 키에 대응하는 A타입 값</returns>
        /// <exception cref="KeyNotFoundException">키가 존재하지 않을 때 발생</exception>
        public TKeyA GetValue_B(TKeyB keyB)
        {
            return Dictionary_B[keyB];
        }



        ///======================================================================================================================================================



        /// <summary>
        /// A타입 키(<paramref name="keyA"/>)로부터 B타입 값을 안전하게 조회합니다.
        /// </summary>
        /// <param name="keyA">찾을 A타입 키</param>
        /// <param name="resultKeyB">찾아진 B타입 값을 담을 out 파라미터</param>
        /// <remarks>키가 없으면 <paramref name="resultKeyB"/>는 기본값(default)이 됩니다.</remarks>
        public bool TryGetValue_A(TKeyA keyA, out TKeyB resultKeyB)
        {
            return Dictionary_A.TryGetValue(keyA, out resultKeyB);
        }

        /// <summary>
        /// B타입 키(<paramref name="keyB"/>)로부터 A타입 값을 안전하게 조회합니다.
        /// </summary>
        /// <param name="keyB">찾을 B타입 키</param>
        /// <param name="resultKeyA">찾아진 A타입 값을 담을 out 파라미터</param>
        /// <remarks>키가 없으면 <paramref name="resultKeyA"/>는 기본값(default)이 됩니다.</remarks>
        public bool TryGetValue_B(TKeyB keyB, out TKeyA resultKeyA)
        {
            return Dictionary_B.TryGetValue(keyB, out resultKeyA);
        }



        ///======================================================================================================================================================



        public void Clear()
        {
            Dictionary_A.Clear();
            Dictionary_B.Clear();
        }


        ///======================================================================================================================================================
    }



    /// <summary>
    /// <see cref="Type"/>을 Key로, 해당 클래스를 Value로 사용하는 자동 생성 딕셔너리입니다.
    /// <para>Generics 제약조건 <typeparamref name="T"/>는 class 타입이어야 합니다.</para>
    /// </summary>
    /// <typeparam name="T">Dictionary의 Value 타입 (class)</typeparam>
    [Serializable]
    public class AutoTypeDictionary<T> where T : class
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 기본 생성자.
        /// <para>내부에서 <c>SU_Collection_Types.SetTypeDictionary_Mode1</c> 호출을 통해 
        /// <see cref="TypesDictionary"/>를 초기화합니다.</para>
        /// </summary>
        public AutoTypeDictionary()
        {
            SU_Collection_Types.CreateTypeDictionary(SU_Collection_Types.TypeSearch.ConcreteClasses, out TypesDictionary);
        }



        /// <summary>
        /// 생성 시점에 추가 액션(<paramref name="action"/>)을 수행하며 
        /// <see cref="TypesDictionary"/>를 초기화하는 생성자입니다.
        /// </summary>
        /// <param name="action">딕셔너리 초기화 시점에 실행할 추가 액션</param>
        public AutoTypeDictionary(Action<T> action)
        {
            SU_Collection_Types.CreateTypeDictionary(SU_Collection_Types.TypeSearch.ConcreteClasses, out TypesDictionary, action);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 내부 Dictionary를 <c>Dictionary&lt;Type, T&gt;</c>로 암묵적 변환하는 연산자입니다.
        /// </summary>
        /// <param name="value">암묵적 변환 대상인 <see cref="AutoTypeDictionary{T}"/></param>
        /// <returns>내부에서 관리하는 <see cref="TypesDictionary"/>를 반환</returns>
        public static implicit operator Dictionary<Type, T>(AutoTypeDictionary<T> value)
            => value.TypesDictionary;



        ///======================================================================================================================================================



        /// <summary>
        /// Type을 Key로, T를 Value로 사용하는 내부 Dictionary입니다.
        /// </summary>
        [ShowInInspector, Searchable]
        protected readonly Dictionary<Type, T> TypesDictionary;



        /// <summary>
        /// 내부 Dictionary를 읽기 전용으로 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<Type, T> GetDictionary => TypesDictionary;



        ///======================================================================================================================================================



        /// <summary>
        /// 제네릭 타입(<typeparamref name="TValue"/>)을 Key로 사용하여 
        /// 해당 값을 <typeparamref name="TValue"/>로 반환합니다.
        /// <para>찾는 값이 없으면 예외가 발생할 수 있습니다.</para>
        /// </summary>
        /// <typeparam name="TValue">
        /// 반환받을 구체적인 클래스 타입 (new() 가능, T를 상속)
        /// </typeparam>
        /// <returns>
        /// <paramref name="TValue"/> 형식에 해당하는 Dictionary의 Value
        /// </returns>
        public TValue Get<TValue>() where TValue : class, T, new()
        {
            return TypesDictionary[typeof(TValue)] as TValue;
        }



        /// <summary>
        /// 제네릭 타입(<typeparamref name="TValue"/>)을 Key로 하여 Dictionary에서 제거합니다.
        /// </summary>
        /// <typeparam name="TValue">
        /// 제거할 구체적인 클래스 타입 (new() 가능, T를 상속)
        /// </typeparam>
        /// <returns>
        /// 제거 성공 시 <c>true</c>, 해당 키가 없거나 제거 실패 시 <c>false</c>
        /// </returns>
        public bool Remove<TValue>() where TValue : class, T, new()
        {
            return TypesDictionary.Remove(typeof(TValue));
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// <see cref="Type"/>과 <see cref="string"/> 모두를 Key로 사용하고,
    /// 공통 <typeparamref name="T"/> 타입의 객체를 Value로 사용하는 자동 생성 딕셔너리입니다.
    /// <para>
    /// <typeparamref name="T"/>는 <see cref="IName"/> 인터페이스를 구현해야 하며,
    /// 각 객체의 <c>ThisName</c> 속성이 문자열 키로 사용됩니다.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Dictionary에 저장될 클래스 타입 (IName 인터페이스 구현)</typeparam>
    [Serializable]
    public class AutoTypeStringDictionary<T> where T : class, IName
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 기본 생성자.
        /// <para>내부에서 <c>SU_Collection_Types.SetTypeDictionary_Mode1</c>를 호출하여 
        /// <see cref="TypesDictionary"/>를 초기화하고,
        /// 그 결과를 바탕으로 <see cref="StringDictionary"/>를 구성합니다.</para>
        /// </summary>
        public AutoTypeStringDictionary()
        {
            //. Type 키로 사용하는 Dictionary 초기화
            SU_Collection_Types.CreateTypeDictionary(SU_Collection_Types.TypeSearch.ConcreteClasses, out TypesDictionary);

            //. String 키로 사용하는 Dictionary 초기화 (TypesDictionary에서 이름을 추출)
            StringDictionary = new Dictionary<string, T>(TypesDictionary.Count);
            foreach (var item in TypesDictionary)
            {
                var key = item.Value.CurrentName;
                var value = item.Value;
                StringDictionary.Add(key, value);
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// Type 키 -> T Value 딕셔너리
        /// </summary>
        [ShowInInspector, Searchable]
        protected readonly Dictionary<Type, T> TypesDictionary;

        /// <summary>
        /// String 키 -> T Value 딕셔너리
        /// </summary>
        [ShowInInspector, Searchable]
        protected readonly Dictionary<string, T> StringDictionary;



        /// <summary>
        /// Type 키를 사용하는 내부 Dictionary를 읽기 전용으로 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<Type, T> GetTypeDictionary => TypesDictionary;



        /// <summary>
        /// String 키를 사용하는 내부 Dictionary를 읽기 전용으로 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<string, T> GetStringDictionary => StringDictionary;



        ///======================================================================================================================================================



        /// <summary>
        /// 지정한 <typeparamref name="TValue"/> 타입의 객체를 Type 키로 얻어옵니다.
        /// </summary>
        /// <typeparam name="TValue">검색할 객체 타입(클래스, IName, new() 제약)</typeparam>
        /// <returns>
        /// <paramref name="TValue"/> 형식에 해당하는 값. 
        /// 존재하지 않으면 <see cref="KeyNotFoundException"/> 발생
        /// </returns>
        public TValue Get<TValue>() where TValue : class, T, new()
        {
            return TypesDictionary[typeof(TValue)] as TValue;
        }



        /// <summary>
        /// 지정된 문자열(<paramref name="key"/>)로 등록된 객체를 얻어옵니다.
        /// </summary>
        /// <param name="key">객체의 <c>ThisName</c>에 해당하는 문자열 키</param>
        /// <returns>문자열 키에 해당하는 <typeparamref name="T"/> 객체</returns>
        public T Get(string key)
        {
            return StringDictionary[key];
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    //? 열거형 딕셔너리 확장



    /// <summary>
    /// (enum ↔ int) 양방향 매핑 전용 Dictionary.
    /// <para>1) 딕셔너리 크기는 해당 열거형의 크기와 동일하게 고정</para>
    /// <para>2) 중복 int(값) 불허</para>
    /// <para>3) "최초 지정"과 "수정 지정" 방식을 분리하여, 중복 에러 처리/스왑 등을 지원</para>
    /// </summary>
    /// <typeparam name="TEnum">관리 대상 enum 타입</typeparam>
    [Serializable]
    public class EnumIntBiDictionary<TEnum> where TEnum : struct, Enum
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="autoInitialize">
        /// <para>true라면, enum에 정의된 기본 int 값을 해당 enum의 Index로 바로 등록 (중복 있으면 예외)</para>
        /// <para>false라면, 아무것도 등록하지 않고 시작</para>
        /// </param>
        public EnumIntBiDictionary(bool autoInitialize = false)
        {
            var enumValues = (TEnum[])Enum.GetValues(typeof(TEnum));
            int capacity = enumValues.Length;


            //? enum->int? (null 가능) 
            EnumToIntDictionary = new Dictionary<TEnum, int?>(capacity);
            //? 열거형 딕셔너리를 먼저 전부 "null 미할당 상태"로 초기화한다
            foreach (var eVal in enumValues) { EnumToIntDictionary[eVal] = null; }


            //? int->enum
            IntToEnumDictionary = new Dictionary<int, TEnum>(capacity);

            foreach (var eVal in enumValues)
            {
                int underlying = (int)(ValueType)(object)eVal;

                // 중복 체크 & 등록
                // 만약 underlying이 이미 다른 enum에 배정되어 있으면 예외
                if (IntToEnumDictionary.ContainsKey(underlying))
                {
                    throw new InvalidOperationException($"[autoInitialize] 중복 int 발생: enum '{IntToEnumDictionary[underlying]}' 과(와) enum '{eVal}'가 동일 int({underlying})를 사용하려 합니다.");
                }

                // 등록
                //. autoInitialize == true 면, 각 enum을 (int)(object)값으로 "최초 지정" 시도
                if (autoInitialize)
                {
                    EnumToIntDictionary[eVal] = underlying;
                    IntToEnumDictionary[underlying] = eVal;
                }
            }
        }



        ///======================================================================================================================================================



        //? 핵심 딕셔너리 & 얻기



        ///<summary>
        /// 열거형의 Int?정보 를 저장하는 딕셔너리
        /// <para>생성자에서 초기화되어 모든 열거형 값이 추가된 상태로 생성된다, (크기 고정)</para>
        /// </summary>
        private readonly Dictionary<TEnum, int?> EnumToIntDictionary;//. null 이면 "해당 enum에 int가 아직 지정되지 않음", 어떤 값이면 "해당 enum의 배정된 int"

        ///<summary>
        /// Int정보에 해당하는 열거형을 저장하는 딕셔너리
        /// <para>열거형의 Int정보가 추가 될때, 이 딕셔너리에 추가가 된다</para>
        /// <para>따라서 모든 열거형의 값이 존재하지않고, 추가된 값들만 딕셔너리에 존재한다</para>
        /// </summary>
        private readonly Dictionary<int, TEnum> IntToEnumDictionary; //. 만약 어떤 int가 배정되지 않았다면 key 자체가 존재하지 않음



        ///<summary>
        /// 열거형의 Int?정보 를 저장하는 딕셔너리
        /// <para>생성자에서 초기화되어 모든 열거형 값이 추가된 상태로 생성된다, (크기 고정)</para>
        /// </summary>
        public IReadOnlyDictionary<TEnum, int?> GetEnumToIntDictionary => EnumToIntDictionary;

        ///<summary>
        /// Int정보에 해당하는 열거형을 저장하는 딕셔너리
        /// <para>열거형의 Int정보가 추가 될때, 이 딕셔너리에 추가가 된다</para>
        /// <para>따라서 모든 열거형의 값이 존재하지않고, 추가된 값들만 딕셔너리에 존재한다</para>
        /// </summary>
        public IReadOnlyDictionary<int, TEnum> GetIntToEnumDictionary => IntToEnumDictionary;



        /// <summary>
        /// 등록되어있는 열거형의 Int정보들의 콜렉션을 반환한다
        /// </summary>
        public IReadOnlyCollection<int> GetAssignedInts => IntToEnumDictionary.Keys;



        ///======================================================================================================================================================



        //? 지정: 최초



        /// <summary>
        /// 특정 enum에 대해, 아직 할당되지 않은 int를 "최초"로 지정합니다.
        /// <para>이미 enum / int 둘 중 하나라도 사용 중이면 예외를 던집니다.</para>
        /// </summary>
        /// <param name="enumKey">할당할 enum 값</param>
        /// <param name="intValue">연결할 int 값</param>
        public void InitializeMapping(TEnum enumKey, int intValue)
        {
            // 1) enumKey가 이미 어떤 int를 가지고 있으면 에러
            if (EnumToIntDictionary[enumKey] != null)
            {
                throw new InvalidOperationException(
                    $"[InitializeMapping] enum '{enumKey}' 는 이미 int({EnumToIntDictionary[enumKey]})를 가지고 있습니다."
                );
            }
            // 2) intValue가 이미 다른 enum에 사용 중이면 에러
            if (IntToEnumDictionary.ContainsKey(intValue))
            {
                throw new InvalidOperationException(
                    $"[InitializeMapping] int '{intValue}' 는 이미 enum '{IntToEnumDictionary[intValue]}' 에 배정되어 있습니다."
                );
            }

            // 문제 없으니 등록
            EnumToIntDictionary[enumKey] = intValue;
            IntToEnumDictionary[intValue] = enumKey;
        }



        //? 지정: 수정



        /// <summary>
        /// 특정 enum의 int 값을 새로이 변경(수정)합니다.
        /// <para>이미 해당 enum이 다른 int를 가지고 있어도 그대로 덮어씁니다.</para>
        /// <para>만약 새 int가 다른 enum에 배정되어 있다면, <paramref name="allowSwapIfConflict"/>에 따라</para>
        /// <para> - false : 예외 발생</para>
        /// <para> - true : 서로 int 값을 교환(swap)</para>
        /// </summary>
        /// <param name="enumKey">수정할 enum 값</param>
        /// <param name="newIntValue">새로 배정할 int 값</param>
        /// <param name="allowSwapIfConflict">충돌 시 swap 허용 여부</param>
        public void ModifyMapping(TEnum enumKey, int newIntValue, bool allowSwapIfConflict = false)
        {
            //. oldIntValue: enumKey가 현재 가지고 있던 int (null 일 수도 있음)
            int? oldIntValue = EnumToIntDictionary[enumKey];

            //. 이미 newIntValue가 다른 enum에 배정되어 있는지 확인
            if (IntToEnumDictionary.TryGetValue(newIntValue, out TEnum conflictEnum))
            {
                // [Case A] 같은 enumKey가 같은 값 => 변경할 필요가 없음.
                if (EqualityComparer<TEnum>.Default.Equals(conflictEnum, enumKey))
                {
                    // 수정할 필요가 없으므로 그냥 반환.
                    return;
                }

                // [Case B] 다른 enum이 사용 중
                if (!allowSwapIfConflict)
                {
                    // swap 불가이므로 예외
                    throw new InvalidOperationException(
                        $"[ModifyMapping] int '{newIntValue}' 는 이미 enum '{conflictEnum}' 에 할당되어 있습니다. 충돌 발생!"
                    );
                }
                else
                {
                    // swap 허용이면 => conflictEnum과 enumKey의 int를 맞교환

                    // conflictEnum 이 가진 기존 int: newIntValue(현재)
                    // enumKey 가 가진 기존 int: oldIntValue(모를 수도 있음)
                    // 1) conflictEnum -> oldIntValue
                    EnumToIntDictionary[conflictEnum] = oldIntValue;
                    if (oldIntValue.HasValue)
                    {
                        IntToEnumDictionary[oldIntValue.Value] = conflictEnum;
                    }
                    else
                    {
                        // oldIntValue가 null이면, conflictEnum은 이제 int 할당 해제
                        // => int->enum 맵에서 "newIntValue"만 제거하고, null 할당
                        IntToEnumDictionary.Remove(newIntValue);
                    }

                    // 2) enumKey -> newIntValue
                    EnumToIntDictionary[enumKey] = newIntValue;
                    IntToEnumDictionary[newIntValue] = enumKey;

                    return;
                }
            }

            //. [Case C] newIntValue가 아무도 안 쓰는 값이면 그대로 덮어쓰기
            //  - 만약 enumKey가 기존 oldIntValue를 가지고 있었다면, int->enum 매핑에서 제거해야 함
            if (oldIntValue.HasValue)
            {
                // int->enum에서 "oldIntValue"를 제거
                IntToEnumDictionary.Remove(oldIntValue.Value);
            }

            // 새 값 등록
            EnumToIntDictionary[enumKey] = newIntValue;
            IntToEnumDictionary[newIntValue] = enumKey;
        }



        ///======================================================================================================================================================



        //? 조회



        /// <summary>
        /// 특정 enum이 어떤 int를 가지는지 조회 (null일 수 있음)
        /// </summary>
        public int? GetIntOrNull(TEnum enumKey)
        {
            return EnumToIntDictionary[enumKey];
        }

        /// <summary>
        /// 특정 enum이 어떤 int를 가지는지 조회 (null일 수 있음)
        /// </summary>
        public bool TryGetIntOrNull(TEnum enumKey, out int? result)
        {
            result = GetIntOrNull(enumKey);
            return result.HasValue;
        }



        /// <summary>
        /// 특정 int가 어떤 enum에 대응하는지 조회 (없으면 null 반환)
        /// </summary>
        public TEnum? GetEnumOrNull(int intValue)
        {
            if (IntToEnumDictionary.TryGetValue(intValue, out var eVal))
            {
                return eVal;
            }
            return null;
        }

        /// <summary>
        /// 특정 int가 어떤 enum에 대응하는지 조회 (없으면 null 반환)
        /// </summary>
        public bool TryGetEnumOrNull(int intValue, out TEnum? result)
        {
            result = GetEnumOrNull(intValue);
            return result.HasValue;
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 열거형-인덱스 딕셔너리를 관리하는 매니저 클래스의 인터페이스
    /// <para>별도로 Enum 제네릭을 사용하지 않고도 사용이 가능하다</para>
    /// <para>하지만 완벽한 제약으로 불러오는것이 아니기에, 사용에 주의가 필요하다</para>
    /// </summary>
    public interface IBaseEnumIndexesManager
    {
        /// <summary>
        /// Index 얻는다
        /// <para><b>열거형의 제약을 완벽하게 받지 않아, 사용에 주의가 필요</b></para>
        /// </summary>
        /// <param name="enumKey"></param>
        /// <returns></returns>
        int GetIndex<TEnum>(TEnum enumKey) where TEnum : struct, Enum;

        /// <summary>
        /// Enum을 얻는다
        /// <para><b>열거형의 제약을 완벽하게 받지 않아, 사용에 주의가 필요</b></para>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        TEnum GetEnum<TEnum>(int index) where TEnum : struct, Enum;

        /// <summary>
        /// Enum의 이름을 얻는다
        /// <para><b>열거형의 제약을 완벽하게 받지 않아, 사용에 주의가 필요</b></para>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        string GetEnumName(int index);

        /// <summary>
        /// 등록된 열거형의 Index 콜렉션을 불러온다
        /// </summary>
        IReadOnlyCollection<int> GetAssignedIndexes { get; }
    }



    /// <summary>
    /// 열거형-인덱스 딕셔너리를 관리하는 매니저 클래스
    /// </summary>
    /// <typeparam name="TEnum">대상 열거형</typeparam>
    [Serializable]
    public abstract class BaseEnumIndexesManager<TEnum> : IBaseEnumIndexesManager where TEnum : struct, Enum
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="autoInitialize">
        /// <para>true라면, enum에 정의된 기본 int 값을 해당 enum의 Index로 바로 등록 (중복 있으면 예외)</para>
        /// <para>false라면, 아무것도 등록하지 않고 시작</para>
        /// </param>
        public BaseEnumIndexesManager(bool autoInitialize)
        {
            EnumIndexes = new EnumIntBiDictionary<TEnum>(autoInitialize);
        }



        ///======================================================================================================================================================



        //? 딕셔너리



        private readonly EnumIntBiDictionary<TEnum> EnumIndexes;

        public IReadOnlyDictionary<TEnum, int?> GetEnumIndexes => EnumIndexes.GetEnumToIntDictionary;

        /// <inheritdoc/>
        public IReadOnlyCollection<int> GetAssignedIndexes => EnumIndexes.GetAssignedInts;



        ///======================================================================================================================================================



        //? 지정: 최초



        /// <summary>
        /// 특정 enum에 대해, 아직 할당되지 않은 index를 "최초"로 지정합니다.
        /// <para>이미 enum / index 둘 중 하나라도 사용 중이면 예외를 던집니다.</para>
        /// </summary>
        /// <param name="enumKey">할당할 enum 값</param>
        /// <param name="index">연결할 int 값</param>
        public void Initialize(TEnum enumKey, int index)
        {
            EnumIndexes.InitializeMapping(enumKey, index);
        }



        //? 지정: 수정



        /// <summary>
        /// 특정 enum의 index 값을 새로이 변경(수정)합니다.
        /// <para>이미 해당 enum이 다른 int를 가지고 있어도 그대로 덮어씁니다.</para>
        /// <para>만약 새 index가 다른 enum에 배정되어 있다면, <paramref name="allowSwapIfConflict"/>에 따라</para>
        /// <para> - false : 예외 발생</para>
        /// <para> - true : 서로 int 값을 교환(swap)</para>
        /// </summary>
        /// <param name="enumKey">수정할 enum 값</param>
        /// <param name="index">새로 배정할 int 값</param>
        /// <param name="allowSwapIfConflict">충돌 시 swap 허용 여부</param>
        public void Modify(TEnum enumKey, int index, bool allowSwapIfConflict = false)
        {
            EnumIndexes.ModifyMapping(enumKey, index, allowSwapIfConflict);
        }



        ///======================================================================================================================================================



        //? 조회



        /// <summary>
        /// Index 얻기
        /// </summary>
        /// <param name="enumKey"></param>
        /// <returns></returns>
        public int GetIndex(TEnum enumKey)
        {
            return EnumIndexes.GetIntOrNull(enumKey).Value;
        }



        /// <summary>
        /// Index 얻어보기
        /// </summary>
        /// <param name="enumKey"></param>
        /// <returns></returns>
        public bool TryGetIndex(TEnum enumKey, out int? resultIndex)
        {
            return EnumIndexes.TryGetIntOrNull(enumKey, out resultIndex);
        }



        /// <summary>
        /// Enum 얻기
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TEnum GetEnum(int index)
        {
            return EnumIndexes.GetEnumOrNull(index).Value;
        }



        /// <summary>
        /// Enum 얻어보기
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool TryGetEnum(int index, out TEnum? resultEnum)
        {
            return EnumIndexes.TryGetEnumOrNull(index, out resultEnum);
        }



        /// <inheritdoc/>
        public int GetIndex<TTEnum>(TTEnum enumKey) where TTEnum : struct, Enum
        {
            return GetIndex((TEnum)(object)enumKey);
        }



        /// <inheritdoc/>
        public TTEnum GetEnum<TTEnum>(int index) where TTEnum : struct, Enum
        {
            return (TTEnum)(object)GetEnum(index);
        }



        /// <inheritdoc/>
        public string GetEnumName(int index)
        {
            //return GetEnum(index).ToString();
            //! 250326, 존재하지않으면 에러가 나와버려서, 존재하지않다면 빈 string을 반환하도록

            var currentEnum = EnumIndexes.GetEnumOrNull(index);
            return (currentEnum.HasValue) ? currentEnum.Value.ToString() : "";
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    //? 최적화 위주 커스텀 콜렉션



    /// <summary>
    /// IndexedSet&lt;T&gt;는 List와 Dictionary의 장점을 결합한 자료구조입니다.<br/>
    /// 내부 List는 순회 성능(캐시 친화적)을 제공하며, Dictionary는 각 요소의 인덱스를 저장하여<br/>
    /// 삽입, 삭제, 검색 연산을 O(1) 시간에 처리할 수 있도록 지원합니다.<br/>
    /// 요소의 순서는 보장되지 않으며, 중복 추가는 허용되지 않습니다.<br/>
    /// </summary>
    /// <typeparam name="T">저장할 요소의 타입</typeparam>
    [Serializable]
    public class IndexedSet<T> : IEnumerable<T>
    {
        ///======================================================================================================================================================



        /// <summary>
        /// IndexedSet의 새 인스턴스를 초기화합니다.
        /// </summary>
        public IndexedSet()
        {
            _items = new List<T>();
            _itemIndices = new Dictionary<T, int>();
        }



        /// <summary>
        /// IndexedSet의 새 인스턴스를 초기화합니다.
        /// </summary>
        public IndexedSet(int capacity)
        {
            _items = new List<T>(capacity);
            _itemIndices = new Dictionary<T, int>(capacity);
        }



        ///======================================================================================================================================================



        //? 내부 List: 요소들을 순차적으로 저장하여 빠른 순회 성능을 제공합니다.
        [DisableIf("@true"), ShowInInspector, ReadOnlyCustom]
        private readonly List<T> _items;



        // Dictionary: 각 요소와 해당 요소가 저장된 인덱스를 관리합니다.
        [DisableIf("@true"), OdinSerialize, ShowInInspector]
        private readonly Dictionary<T, int> _itemIndices;



        ///======================================================================================================================================================



        /// <summary>
        /// IndexedSet에 요소를 추가합니다.
        /// 이미 존재하는 요소라면 추가하지 않고 false를 반환합니다.
        /// </summary>
        /// <param name="item">추가할 요소</param>
        /// <returns>요소가 추가되면 true, 중복으로 인해 추가되지 않으면 false</returns>
        public bool Add(T item)
        {
            if (_itemIndices.ContainsKey(item))
                return false;

            _items.Add(item);
            _itemIndices[item] = _items.Count - 1;
            return true;
        }



        /// <summary>
        /// 지정한 여러 요소들을 IndexedSet에 한 번에 추가합니다.
        /// 이미 존재하는 요소는 추가하지 않습니다.
        /// </summary>
        /// <param name="items">추가할 요소들 (params 배열)</param>
        public void AddRange(params T[] items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
            {
                Add(item);
            }
        }



        /// <summary>
        /// 지정한 IList&lt;T&gt;에 포함된 여러 요소들을 IndexedSet에 한 번에 추가합니다.
        /// 이미 존재하는 요소는 추가하지 않습니다.
        /// </summary>
        /// <param name="items">추가할 요소들의 IList</param>
        public void AddRange(IList<T> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            for (int i = 0; i < items.Count; i++)
            {
                Add(items[i]);
            }
        }


        ///======================================================================================================================================================



        /// <summary>
        /// IndexedSet에서 지정한 요소를 제거합니다.
        /// 요소가 존재하면 제거 후 true를 반환합니다.
        /// </summary>
        /// <param name="item">제거할 요소</param>
        /// <returns>요소가 제거되었으면 true, 요소가 존재하지 않으면 false</returns>
        public bool Remove(T item)
        {
            if (!_itemIndices.TryGetValue(item, out int index))
                return false;

            RemoveAt(index);
            return true;
        }



        /// <summary>
        /// 지정한 여러 요소들을 IndexedSet에서 한 번에 제거합니다.
        /// 존재하지 않는 요소는 무시합니다.
        /// </summary>
        /// <param name="items">제거할 요소들 (params 배열)</param>
        public void RemoveRange(params T[] items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
            {
                Remove(item);
            }
        }



        /// <summary>
        /// 지정한 IList&lt;T&gt;에 포함된 여러 요소들을 IndexedSet에서 한 번에 제거합니다.
        /// 존재하지 않는 요소는 무시합니다.
        /// </summary>
        /// <param name="items">제거할 요소들의 IList</param>
        public void RemoveRange(IList<T> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            for (int i = 0; i < items.Count; i++)
            {
                Remove(items[i]);
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 지정한 인덱스의 요소를 제거합니다.
        /// 내부적으로 스왑 삭제 기법을 사용하여 O(1) 시간에 요소를 제거합니다.
        /// </summary>
        /// <param name="index">제거할 요소의 인덱스</param>
        /// <exception cref="ArgumentOutOfRangeException">인덱스가 유효하지 않은 경우</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _items.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "인덱스가 범위를 벗어났습니다.");

            // 제거할 요소와 마지막 요소를 가져옴
            T itemToRemove = _items[index];
            int lastIndex = _items.Count - 1;
            T lastItem = _items[lastIndex];

            if (index != lastIndex)
            {
                // 마지막 요소를 제거할 위치로 옮기고, Dictionary를 업데이트
                _items[index] = lastItem;
                _itemIndices[lastItem] = index;
            }

            // List의 마지막 요소를 제거하고, Dictionary에서도 해당 항목 제거
            _items.RemoveAt(lastIndex);
            _itemIndices.Remove(itemToRemove);
        }



        /// <summary>
        /// 지정한 여러 인덱스에 해당하는 요소들을 IndexedSet에서 한 번에 제거합니다.
        /// 인덱스는 내부 List의 순서를 따르며, 인덱스들이 중복되거나 범위를 벗어나면 예외가 발생합니다.
        /// </summary>
        /// <param name="indices">제거할 요소들의 인덱스들 (params int 배열)</param>
        public void RemoveAtRange(params int[] indices)
        {
            if (indices == null)
                throw new ArgumentNullException(nameof(indices));

            // 인덱스 배열을 복사한 후, 내림차순으로 정렬하여 삭제 시 인덱스 변화에 영향받지 않도록 함
            int[] sortedIndices = (int[])indices.Clone();
            Array.Sort(sortedIndices);
            Array.Reverse(sortedIndices);

            foreach (var index in sortedIndices)
            {
                RemoveAt(index);
            }
        }



        /// <summary>
        /// 지정한 IList&lt;int&gt;에 포함된 여러 인덱스에 해당하는 요소들을 IndexedSet에서 한 번에 제거합니다.
        /// 인덱스는 내부 List의 순서를 따르며, 인덱스들이 중복되거나 범위를 벗어나면 예외가 발생합니다.
        /// </summary>
        /// <param name="indices">제거할 요소들의 인덱스들이 담긴 IList</param>
        public void RemoveAtRange(IList<int> indices)
        {
            if (indices == null)
                throw new ArgumentNullException(nameof(indices));

            // IList<int>를 List<int>로 복사 후 내림차순 정렬
            List<int> sortedIndices = new List<int>(indices);
            sortedIndices.Sort();
            sortedIndices.Reverse();

            foreach (var index in sortedIndices)
            {
                RemoveAt(index);
            }
        }



        ///======================================================================================================================================================



        ///<summary>
        ///사용하지않는 용량 제거
        ///</summary>
        public void TrimExcess()
        {
            _items.TrimExcess();
            _itemIndices.TrimExcess();
        }



        ///======================================================================================================================================================



        /// <summary>
        /// IndexedSet에 지정한 요소가 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="item">확인할 요소</param>
        /// <returns>요소가 존재하면 true, 그렇지 않으면 false</returns>
        public bool Contains(T item)
        {
            return _itemIndices.ContainsKey(item);
        }



        /// <summary>
        /// 지정한 요소의 인덱스를 반환하려 시도합니다.
        /// </summary>
        /// <param name="item">찾을 요소</param>
        /// <param name="index">요소가 존재할 경우 해당 요소의 인덱스가 저장됨</param>
        /// <returns>요소가 존재하면 true, 그렇지 않으면 false</returns>
        public bool TryGetIndex(T item, out int index)
        {
            return _itemIndices.TryGetValue(item, out index);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 인덱스를 사용하여 IndexedSet의 요소에 접근합니다.
        /// </summary>
        /// <param name="index">접근할 요소의 인덱스</param>
        /// <returns>해당 인덱스의 요소</returns>
        /// <exception cref="ArgumentOutOfRangeException">인덱스가 유효하지 않은 경우</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _items.Count)
                    throw new ArgumentOutOfRangeException(nameof(index), "인덱스가 범위를 벗어났습니다.");
                return _items[index];
            }
        }



        /// <summary>
        /// IndexedSet에 저장된 요소의 개수를 반환합니다.
        /// </summary>
        public int Count => _items.Count;



        ///======================================================================================================================================================


        /// <summary>
        /// IndexedSet의 모든 요소를 제거합니다.
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            _itemIndices.Clear();
        }



        ///======================================================================================================================================================



        /// <summary>
        /// IndexedSet의 열거자를 반환합니다.
        /// 내부 List의 순서를 따르므로, 요소 순서는 보장되지 않습니다.
        /// </summary>
        /// <returns>요소를 순회하는 열거자</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }



        /// <summary>
        /// IndexedSet의 열거자를 반환합니다.
        /// </summary>
        /// <returns>요소를 순회하는 열거자</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }



        /// <summary>
        /// IndexedSet의 리스트를 읽기전용으로 반환
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<T> GetReadOnlyItems()
        {
            return _items;
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 특정 데이터의 불변(Immutable) 캐싱을 위한 범용 매니저 클래스
    /// </summary>
    /// <typeparam name="TKey">검색할 Key 타입 (예: string, int 등)</typeparam>
    /// <typeparam name="TValue">저장할 Value 타입 (불변 데이터여야 함)</typeparam>
    [Serializable]
    public class ImmutableDataManager<TKey, TValue> where TKey : notnull
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 불변 데이터를 초기화하여 저장하는 생성자
        /// </summary>
        /// <param name="dataEnumerable">저장할 데이터 컬렉션</param>
        /// <param name="keySelector">각 데이터에서 Key 값을 추출하는 함수</param>
        /// <exception cref="ArgumentNullException">입력 값이 null인 경우 예외 발생</exception>
        /// <exception cref="ArgumentException">중복된 키가 존재하는 경우 예외 발생</exception>
        public ImmutableDataManager(IEnumerable<TValue> dataEnumerable, Func<TValue, TKey> keySelector)
        {
            //? null 체크
            if (dataEnumerable == null) throw new ArgumentNullException(nameof(dataEnumerable));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            //? IEnumerable<T>를 IList<T> 또는 List<T>로 변환 (최적화)
            var list = dataEnumerable as IList<TValue> ?? dataEnumerable.ToList();
            int count = list.Count;

            //? 데이터 크기만큼 배열과 딕셔너리 초기화
            dataArray = new TValue[count];
            indexDictionary = new Dictionary<TKey, int>(count);

            //? 데이터를 배열과 딕셔너리에 저장
            for (int i = 0; i < count; i++)
            {
                var item = list[i];
                var key = keySelector(item);

                //! 중복 키가 존재하면 예외 발생
                if (indexDictionary.ContainsKey(key))
                    throw new ArgumentException($"중복된 키: {key}");

                dataArray[i] = item;     // 배열에 저장
                indexDictionary[key] = i; // 딕셔너리에 인덱스 저장
            }
        }



        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var result))
                {
                    return result;
                }
                throw new ArgumentOutOfRangeException(nameof(key), "해당 Key가 존재하지 않습니다.");
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// Key 값과 해당 데이터의 인덱스를 매핑하는 딕셔너리
        /// </summary>
        private readonly Dictionary<TKey, int> indexDictionary;



        /// <summary>
        /// 불변 데이터를 저장하는 배열 (배열 기반 접근으로 빠른 검색 가능)
        /// </summary>
        public readonly TValue[] dataArray;



        /// <summary>
        /// 현재 저장된 데이터의 개수를 반환합니다.
        /// </summary>
        public int Count => dataArray.Length;



        ///======================================================================================================================================================



        /// <summary>
        /// 지정된 인덱스의 데이터를 가져옵니다. (배열을 사용하여 O(1) 속도로 접근 가능)
        /// </summary>
        /// <param name="index">가져올 데이터의 인덱스</param>
        /// <returns>해당 인덱스의 데이터</returns>
        /// <exception cref="IndexOutOfRangeException">잘못된 인덱스에 접근할 경우 발생</exception>
        //? 구조체일 수도 있으므로 'ref readonly'를 사용하여 불변성을 보장하고 복사 비용을 줄임
        public ref readonly TValue GetByIndex(int index)
        {
            //! 인덱스가 유효한 범위인지 확인 (오버플로우 방지를 위해 uint 변환 사용)
            if ((uint)index >= (uint)dataArray.Length)
                throw new IndexOutOfRangeException($"잘못된 인덱스 접근: {index}");

            return ref dataArray[index];
        }



        public bool ContainsKey(TKey key)
        {
            return indexDictionary.ContainsKey(key);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 특정 키(TKey)에 해당하는 데이터를 가져옵니다.
        /// </summary>
        /// <param name="key">검색할 Key 값</param>
        /// <param name="data">찾은 데이터 (없으면 default 값 반환)</param>
        /// <returns>키가 존재하면 true, 없으면 false</returns>
        public bool TryGetValue(TKey key, out TValue data)
        {
            //? indexDictionary에서 key를 조회하여 index를 찾음
            if (indexDictionary.TryGetValue(key, out int index))
            {
                data = dataArray[index]; // 찾은 데이터 반환
                return true;
            }

            data = default; // 키가 존재하지 않으면 기본값 할당
            return false;
        }



        /// <summary>
        /// <b>찾지 못해도 에러를 반환하지 않고 <i>default</i>를 반환함에 주의</b> 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue? GetValue(TKey key)
        {
            if (!TryGetValue(key, out var result))
            {
                return default;
            }
            return result;
        }



        ///======================================================================================================================================================

    }



    /// <summary>
    /// 2개의 서로 다른 Key 타입으로 동일한 TValue를 조회할 수 있는 불변(Immutable) 매니저
    /// </summary>
    /// <typeparam name="TKey1">첫 번째 키 타입</typeparam>
    /// <typeparam name="TKey2">두 번째 키 타입</typeparam>
    /// <typeparam name="TValue">저장할 데이터 타입 (불변이어야 함)</typeparam>
    [Serializable]
    public class ImmutableDataManager2<TKey1, TKey2, TValue>
        where TKey1 : notnull
        where TKey2 : notnull
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 2개의 Key를 모두 따로 관리하는 ImmutableDataManager2 생성자
        /// </summary>
        /// <param name="dataEnumerable">초기화할 데이터 컬렉션</param>
        /// <param name="key1Selector">첫 번째 키 추출 함수</param>
        /// <param name="key2Selector">두 번째 키 추출 함수</param>
        public ImmutableDataManager2(
            IEnumerable<TValue> dataEnumerable,
            Func<TValue, TKey1> key1Selector,
            Func<TValue, TKey2> key2Selector)
        {
            //. null 체크
            if (dataEnumerable == null) throw new ArgumentNullException(nameof(dataEnumerable));
            if (key1Selector == null) throw new ArgumentNullException(nameof(key1Selector));
            if (key2Selector == null) throw new ArgumentNullException(nameof(key2Selector));

            //. IEnumerable -> List 변환
            var list = dataEnumerable as IList<TValue> ?? dataEnumerable.ToList();
            int count = list.Count;

            //. 배열, 딕셔너리 초기화
            dataArray = new TValue[count];
            indexDictionary1 = new Dictionary<TKey1, int>(count);
            indexDictionary2 = new Dictionary<TKey2, int>(count);

            //. 데이터 저장
            for (int i = 0; i < count; i++)
            {
                //. 리스트에서 아이템 꺼내옴
                var item = list[i];

                //. 두 Key 추출
                TKey1 key1 = key1Selector(item);
                TKey2 key2 = key2Selector(item);

                //! 첫 번째 키 중복 검사
                if (indexDictionary1.ContainsKey(key1))
                    throw new ArgumentException($"(Key1) 중복된 키: {key1}");
                //! 두 번째 키 중복 검사
                if (indexDictionary2.ContainsKey(key2))
                    throw new ArgumentException($"(Key2) 중복된 키: {key2}");

                //. 배열에 저장
                dataArray[i] = item;

                //. 딕셔너리에 인덱스 매핑
                indexDictionary1[key1] = i;
                indexDictionary2[key2] = i;
            }
        }



        public TValue this[TKey1 key]
        {
            get
            {
                if (TryGetValue1(key, out var result))
                {
                    return result;
                }
                throw new ArgumentOutOfRangeException(nameof(key), "해당 Key가 존재하지 않습니다.");
            }
        }



        public TValue this[TKey2 key]
        {
            get
            {
                if (TryGetValue2(key, out var result))
                {
                    return result;
                }
                throw new ArgumentOutOfRangeException(nameof(key), "해당 Key가 존재하지 않습니다.");
            }
        }



        ///======================================================================================================================================================



        //? 첫 번째 키 => 인덱스 매핑
        private readonly Dictionary<TKey1, int> indexDictionary1;
        //? 두 번째 키 => 인덱스 매핑
        private readonly Dictionary<TKey2, int> indexDictionary2;



        //? 불변 데이터를 저장할 배열
        public readonly TValue[] dataArray;



        //. 현재 데이터 개수
        public int Count => dataArray.Length;



        ///======================================================================================================================================================



        /// <summary>
        /// 인덱스를 통해 데이터를 가져옴
        /// </summary>
        /// <param name="index">배열 인덱스</param>
        /// <returns>해당 인덱스의 데이터</returns>
        //? 구조체도 고려하므로 'ref readonly' 사용
        public ref readonly TValue GetByIndex(int index)
        {
            //! 인덱스 범위 검사
            if ((uint)index >= (uint)dataArray.Length)
                throw new IndexOutOfRangeException($"잘못된 인덱스 접근: {index}");

            return ref dataArray[index];
        }



        public bool ContainsKey1(TKey1 key)
        {
            return indexDictionary1.ContainsKey(key);
        }



        public bool ContainsKey2(TKey2 key)
        {
            return indexDictionary2.ContainsKey(key);
        }

        ///======================================================================================================================================================



        /// <summary>
        /// 첫 번째 Key를 이용해 데이터를 검색
        /// </summary>
        /// <param name="key1">검색할 첫 번째 키</param>
        /// <param name="data">결과 데이터 (없으면 default)</param>
        /// <returns>키가 존재하면 true</returns>
        public bool TryGetValue1(TKey1 key1, out TValue data)
        {
            if (indexDictionary1.TryGetValue(key1, out int index))
            {
                data = dataArray[index];
                return true;
            }

            data = default;
            return false;
        }



        /// <summary>
        /// 두 번째 Key를 이용해 데이터를 검색
        /// </summary>
        /// <param name="key2">검색할 두 번째 키</param>
        /// <param name="data">결과 데이터 (없으면 default)</param>
        /// <returns>키가 존재하면 true</returns>
        public bool TryGetValue2(TKey2 key2, out TValue data)
        {
            if (indexDictionary2.TryGetValue(key2, out int index))
            {
                data = dataArray[index];
                return true;
            }

            data = default;
            return false;
        }



        public TValue? GetValue1(TKey1 key)
        {
            if (!TryGetValue1(key, out var result))
            {
                return default;
            }
            return result;
        }



        public TValue? GetValue2(TKey2 key)
        {
            if (!TryGetValue2(key, out var result))
            {
                return default;
            }
            return result;
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    //? 타입 인스턴스 공장, (자동생성도 가능)



    ///<summary>
    ///자동 코드 생성 타입 인스턴스 Base 클래스
    /// </summary>
    [Serializable]
    public abstract class BaseTypeInstanceAuto<TClass> : IBaseTypeInstanceAuto<TClass> where TClass : class
    {
        public BaseTypeInstanceAuto(Dictionary<Type, TClass> generatedTypeInstances)
        {
            GeneratedTypeInstances = generatedTypeInstances;
        }

        protected readonly Dictionary<Type, TClass> GeneratedTypeInstances;
        Dictionary<Type, TClass> IBaseTypeInstanceAuto<TClass>.GeneratedTypeInstances => GeneratedTypeInstances;
        public IReadOnlyDictionary<Type, TClass> GetGeneratedTypeInstances => GeneratedTypeInstances;

    }



    ///<summary>
    ///자동 코드 생성 타입 인스턴스 Base 인터페이스
    /// </summary>
    public interface IBaseTypeInstanceAuto<TClass> where TClass : class
    {
        public Dictionary<Type, TClass> GeneratedTypeInstances { get; }
        public IReadOnlyDictionary<Type, TClass> GetGeneratedTypeInstances { get; }
    }



    /// <summary>
    /// <para>
    /// 특정 <typeparamref name="TClass"/>를 상속(또는 구현)하는 타입들을<br/> 
    /// 1) 리플렉션 기반으로 자동 생성하거나 <br/> 
    /// 2) 이미 자동으로 생성된 Dictionary를 주입받아 사용하는 <br/> 
    /// 공통 로직을 제공하는 팩토리 클래스입니다.
    /// </para>
    /// <para>
    /// 게임에서 특정 베이스 클래스를 상속받는 여러 파생 클래스를 관리할 때,<br/> 
    /// 간편하게 <see cref="Dictionary{TKey, TValue}"/> 로 접근하고 싶을 때 활용할 수 있습니다.<br/> 
    /// </para>
    /// </summary>
    /// <typeparam name="TClass">Dictionary에 담길 객체의 베이스 타입 (클래스)</typeparam>
    [Serializable]
    public class TypeInstancesFactory<TClass> where TClass : class
    {
        ///======================================================================================================================================================



        //? 리플렉션 생성



        /// <summary>
        /// <para>
        /// 리플렉션을 사용하여, <typeparamref name="TClass"/>를 상속(또는 구현)하는 모든 클래스를 <br/>
        /// 생성하는 기본 생성자입니다.
        /// </para>
        /// </summary>
        /// <param name="typeSearchOption">검색 옵션 (추상 클래스 포함 여부 등)</param>
        /// <param name="afterEvent">생성된 각 인스턴스마다 후처리를 할 수 있는 액션</param>
        private TypeInstancesFactory(SU_Collection_Types.TypeSearch typeSearchOption = SU_Collection_Types.TypeSearch.ConcreteClasses, Action<TClass> afterEvent = null)
        {
            //! 여기서는 미리 생성된 코드가 없어도 동작 가능해야 합니다.
            //? (typeSearchOption)에 맞춰 TClass를 상속받는 모든 타입을 Reflection으로 찾습니다.
            //. 이후 Dictionary(Type, TClass)에 담아 반환합니다.

            UseGeneratedCode = false;
            SU_Collection_Types.CreateTypeDictionary(
                typeSearchOption,
                out var dic,
                afterEvent
            );
            TypeInstanceDictionary = dic;
        }



        /// <summary>
        /// <para>
        /// 리플렉션을 사용하여, <typeparamref name="TClass"/>를 상속(또는 구현)하는 모든 클래스 생성
        /// </para>
        /// </summary>
        /// <param name="typeSearchOption">검색 옵션 (추상 클래스 포함 여부 등)</param>
        /// <param name="afterEvent">생성된 각 인스턴스마다 후처리를 할 수 있는 액션</param>
        public static TypeInstancesFactory<TClass> Create(SU_Collection_Types.TypeSearch typeSearchOption = SU_Collection_Types.TypeSearch.ConcreteClasses, Action<TClass> afterEvent = null)
        {
            return new TypeInstancesFactory<TClass>(typeSearchOption, afterEvent);
        }



        /// <summary>
        /// <para>
        /// 리플렉션을 사용하여, <typeparamref name="TClass"/>를 상속(또는 구현)하는 모든 클래스 생성
        /// </para>
        /// </summary>
        /// <param name="typeSearchOption">검색 옵션 (추상 클래스 포함 여부 등)</param>
        /// <param name="afterEvent">생성된 각 인스턴스마다 후처리를 할 수 있는 액션</param>
        public static void Create(out TypeInstancesFactory<TClass> result, SU_Collection_Types.TypeSearch typeSearchOption = SU_Collection_Types.TypeSearch.ConcreteClasses, Action<TClass> afterEvent = null)
        {
            result = Create(typeSearchOption, afterEvent);
        }



        ///======================================================================================================================================================



        //? 자동 생성 등록 생성



        /// <summary>
        /// <para>
        /// 자동 생성된 Dictionary를 직접 주입받아 팩토리를 구성하는 내부 생성자입니다.
        /// 외부에서 이미 생성된 <see cref="Dictionary{TKey, TValue}"/>를 할당할 수 있습니다.
        /// </para>
        /// </summary>
        /// <param name="typeInstanceDictionary">이미 준비된 (Type, TClass) Dictionary</param>
        private TypeInstancesFactory(Dictionary<Type, TClass> typeInstanceDictionary)
        {
            //! 여기서는 Reflection 없이, 주어진 dic을 그대로 재활용합니다.
            UseGeneratedCode = true;
            TypeInstanceDictionary = typeInstanceDictionary;
        }



        /// <summary>
        /// <para>
        /// 자동 생성된 클래스(예: AutoCacheTypeInstances_BaseAdvancedSkin)를 
        /// 정적 메서드를 통해 간편하게 생성하여 팩토리를 만들고 싶을 때 사용하는 생성 메서드
        /// </para>
        /// <para>
        /// <typeparamref name="TTypeInstanceAuto"/>는 <see cref="IBaseTypeInstanceAuto{TClass}"/>를 구현하며, 
        /// 기본 생성자가 있어야 합니다.
        /// </para>
        /// </summary>
        /// <typeparam name="TTypeInstanceAuto">
        /// 자동 생성된 클래스 타입.
        /// </typeparam>
        /// <param name="afterEvent">생성된 각 인스턴스마다 후처리를 할 수 있는 액션</param>
        /// <returns>
        /// 자동 생성 클래스로부터 구성된 <see cref="TypeInstancesFactory{TClass}"/>
        /// </returns>
        public static TypeInstancesFactory<TClass> Create<TTypeInstanceAuto>(Action<TClass> afterEvent = null) where TTypeInstanceAuto : class, IBaseTypeInstanceAuto<TClass>, new()
        {
            //. 1) 자동 생성된 클래스를 new로 생성합니다.
            var autoGen = new TTypeInstanceAuto();

            //. 2) 해당 클래스 내부의 CachedTypeInstances를 받아옵니다.
            var dic = autoGen.GeneratedTypeInstances;

            //. 3) afterEvent가 있으면, 이미 생성된 객체 각각에 대해 호출
            if (afterEvent != null)
            {
                foreach (var kv in dic)
                {
                    //? kv는 (Type, TClass) pair. 여기서 Value는 TClass 타입 객체입니다.
                    afterEvent(kv.Value);
                }
            }

            //. 4) private 생성자를 호출해, UseGeneratedCode=true인 팩토리를 반환
            return new TypeInstancesFactory<TClass>(dic);
        }



        /// <summary>
        /// <para>
        /// 자동 생성된 클래스(예: AutoCacheTypeInstances_BaseAdvancedSkin)를 
        /// 정적 메서드를 통해 간편하게 생성하여 팩토리를 만들고 싶을 때 사용하는 생성 메서드
        /// </para>
        /// <para>
        /// <typeparamref name="TTypeInstanceAuto"/>는 <see cref="IBaseTypeInstanceAuto{TClass}"/>를 구현하며, 
        /// 기본 생성자가 있어야 합니다.
        /// </para>
        /// </summary>
        /// <typeparam name="TTypeInstanceAuto">
        /// 자동 생성된 클래스 타입.
        /// </typeparam>
        /// <param name="afterEvent">생성된 각 인스턴스마다 후처리를 할 수 있는 액션</param>
        /// <returns>
        /// 자동 생성 클래스로부터 구성된 <see cref="TypeInstancesFactory{TClass}"/>
        /// </returns>
        public static void Create<TTypeInstanceAuto>(out TypeInstancesFactory<TClass> result, Action<TClass> afterEvent = null) where TTypeInstanceAuto : class, IBaseTypeInstanceAuto<TClass>, new()
        {
            result = Create<TTypeInstanceAuto>(afterEvent);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// <para>딕셔너리에 담긴 (Type, TClass) 쌍을 보관합니다.</para>
        /// <para>Reflection 방식이면 <see cref="SU_Collection_Types.CreateTypeDictionary"/>를 통해 생성됩니다.</para>
        /// <para>자동 생성 방식이면 외부에서 주입받은 <see cref="IBaseTypeInstanceAuto{TClass}"/>를 통해 설정됩니다.</para>
        /// </summary>
        public readonly Dictionary<Type, TClass> TypeInstanceDictionary;



        /// <summary>
        /// <para>생성된 Dictionary가 자동 생성된 코드(=미리 컴파일된 클래스)인지 여부를 나타냅니다.</para>
        /// <para>true라면 <see cref="Create{TAutoCache}(Action{TClass})"/>를 통해 생성된 것이고, 
        /// false라면 Reflection 기반입니다.</para>
        /// </summary>
        public readonly bool UseGeneratedCode;



        ///======================================================================================================================================================
    }



    //? 타입 인스턴스 공장 매니저



    /// <summary>
    /// 타입 인스턴스 공장 매니저의 베이스<br/>
    /// 타입 인스턴스 공장을 단일로 사용해도 좋지만, 어느정도 기본적인 매니저 형태를 별도로 작성하지 않고 편하게 사용하고 싶을때 사용한다
    /// </summary>
    /// <typeparam name="TClass"></typeparam>
    [Serializable]
    public abstract class BaseTypeInstancesFactoryManager<TClass> where TClass : class
    {
        ///======================================================================================================================================================


        /// <summary>
        /// 자식 클래스에서만 사용 가능한 생성자
        /// </summary>
        /// <param name="typeInstancesFactory"></param>
        protected BaseTypeInstancesFactoryManager(TypeInstancesFactory<TClass> typeInstancesFactory)
        {
            TypeInstancesFactory = typeInstancesFactory;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 타입 인스턴스 공장
        /// </summary>
        public readonly TypeInstancesFactory<TClass> TypeInstancesFactory;



        ///======================================================================================================================================================



        /// <summary>
        /// 타입 인스턴스 공장으로부터, 값을 Generic으로 얻기
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetInstance<T>() where T : class, TClass, new()
        {
            return TypeInstancesFactory.TypeInstanceDictionary[typeof(T)] as T;
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 타입 인스턴스 공장 매니저의 베이스<br/>
    /// 타입 인스턴스 공장을 단일로 사용해도 좋지만, 어느정도 기본적인 매니저 형태를 별도로 작성하지 않고 편하게 사용하고 싶을때 사용한다<br/>
    /// 생성에 필요한 인자가 필요없는 new() 타입의 클래스가 대상
    /// </summary>
    /// <typeparam name="TClass"></typeparam>
    [Serializable]
    public class TypeInstancesFactoryManager<TClass> : BaseTypeInstancesFactoryManager<TClass> where TClass : class
    {
        ///======================================================================================================================================================



        protected TypeInstancesFactoryManager(TypeInstancesFactory<TClass> typeInstancesFactory) : base(typeInstancesFactory) { }



        //? 리플렉션 생성


        /// <summary>
        /// 리플렉션으로 생성한다
        /// </summary>
        /// <returns></returns>
        public static TypeInstancesFactoryManager<TClass> Create(SU_Collection_Types.TypeSearch typeSearchOption = SU_Collection_Types.TypeSearch.ConcreteClasses)
        {
            TypeInstancesFactory<TClass>.Create(
                out var typeInstancesFactory,
                typeSearchOption);

            return new TypeInstancesFactoryManager<TClass>(typeInstancesFactory);
        }


        /// <summary>
        /// 리플렉션으로 생성한다
        /// </summary>
        /// <returns></returns>
        public static void Create(out TypeInstancesFactoryManager<TClass> result, SU_Collection_Types.TypeSearch typeSearchOption = SU_Collection_Types.TypeSearch.ConcreteClasses)
        {
            result = Create(typeSearchOption);
        }



        //? 자동 생성 등록 생성


        /// <summary>
        /// 자동 생성 등록으로 생성한다
        /// </summary>
        /// <returns></returns>
        public static TypeInstancesFactoryManager<TClass> Create<TTypeInstanceAuto>()
            where TTypeInstanceAuto : class, IBaseTypeInstanceAuto<TClass>, new()
        {
            var typeInstances = TypeInstancesFactory<TClass>.Create<TTypeInstanceAuto>();

            return new TypeInstancesFactoryManager<TClass>(typeInstances);
        }



        /// <summary>
        /// 자동 생성 등록으로 생성한다
        /// </summary>
        /// <returns></returns>
        public static void Create<TTypeInstanceAuto>(out TypeInstancesFactoryManager<TClass> result)
            where TTypeInstanceAuto : class, IBaseTypeInstanceAuto<TClass>, new()
        {
            result = Create<TTypeInstanceAuto>();
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 타입 인스턴스 공장 매니저의 베이스<br/>
    /// 타입 인스턴스 공장을 단일로 사용해도 좋지만, 어느정도 기본적인 매니저 형태를 별도로 작성하지 않고 편하게 사용하고 싶을때 사용한다<br/>
    /// 생성에 필요한 인자가 한개 필요한 <see cref="IWakeUp{TParam}"/>이 구현되어있는 클래스가 대상,<br/>
    /// 당연히 각 클래스가 생성한 후헤 <see cref="IWakeUp{TParam}.WakeUp(TParam)"/>이 호출된다
    /// </summary>
    /// <typeparam name="TClass"></typeparam>
    /// <typeparam name="TParam">WakeUp에 사용되는 파라미터 인자</typeparam>
    [Serializable]
    public class TypeInstancesFactoryManager<TClass, TParam> : BaseTypeInstancesFactoryManager<TClass>
        where TClass : class, IWakeUp<TParam>
    {
        ///======================================================================================================================================================



        protected TypeInstancesFactoryManager(TypeInstancesFactory<TClass> typeInstancesFactory) : base(typeInstancesFactory) { }



        //? 리플렉션 생성


        /// <summary>
        /// 리플렉션으로 생성한다
        /// </summary>
        /// <param name="param"><see cref="IWakeUp{TParam}"/>에 사용하기 위한 파라미터</param>
        /// <returns></returns>
        public static TypeInstancesFactoryManager<TClass, TParam> Create(TParam param, SU_Collection_Types.TypeSearch typeSearchOption = SU_Collection_Types.TypeSearch.ConcreteClasses)
        {
            var typeInstancesFactory = TypeInstancesFactory<TClass>.Create(typeSearchOption, x => x.WakeUp(param));
            return new TypeInstancesFactoryManager<TClass, TParam>(typeInstancesFactory);
        }



        /// <summary>
        /// 리플렉션으로 생성한다
        /// </summary>
        /// <param name="param"><see cref="IWakeUp{TParam}"/>에 사용하기 위한 파라미터</param>
        /// <returns></returns>
        public static void Create(TParam param, out TypeInstancesFactoryManager<TClass, TParam> result, SU_Collection_Types.TypeSearch typeSearchOption = SU_Collection_Types.TypeSearch.ConcreteClasses)
        {
            result = Create(param, typeSearchOption);
        }



        //? 자동 생성 등록 생성



        /// <summary>
        /// 자동 생성 등록으로 생성한다
        /// </summary>
        /// <param name="param"><see cref="IWakeUp{TParam}"/>에 사용하기 위한 파라미터</param>
        /// <returns></returns>
        public static TypeInstancesFactoryManager<TClass, TParam> Create<TTypeInstanceAuto>(TParam param)
            where TTypeInstanceAuto : class, IBaseTypeInstanceAuto<TClass>, new()
        {
            var typeInstancesFactory = TypeInstancesFactory<TClass>.Create<TTypeInstanceAuto>(x => x.WakeUp(param));
            return new TypeInstancesFactoryManager<TClass, TParam>(typeInstancesFactory);
        }



        /// <summary>
        /// 자동 생성 등록으로 생성한다
        /// </summary>
        /// <param name="param"><see cref="IWakeUp{TParam}"/>에 사용하기 위한 파라미터</param>
        /// <returns></returns>
        public static void Create<TTypeInstanceAuto>(TParam param1, out TypeInstancesFactoryManager<TClass, TParam> result)
            where TTypeInstanceAuto : class, IBaseTypeInstanceAuto<TClass>, new()
        {
            result = Create<TTypeInstanceAuto>(param1);
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    //? 1차원 배열을 2차원 배열처럼 사용



    /// <summary>
    /// 1차원 배열로 저장하면서 2차원 그리드처럼 인덱서로 접근할 수 있는 경량 그리드입니다.
    /// </summary>
    /// <typeparam name="T">셀에 저장될 데이터 타입</typeparam>
    [Serializable]
    public sealed class LinearGrid2D<T>
    {
        ///======================================================================================================================================================



        //? 생성자
        public LinearGrid2D(int width, int height)
        {
            Instance(width, height);
        }



        public void Instance(int width, int height)
        {
            this.width = Mathf.Max(1, width);
            this.height = Mathf.Max(1, height);
            cells = new T[this.width * this.height];
            isValid = true;
        }



        public void Clear()
        {
            width = -1;
            height = -1;
            cells = null;
            isValid = false;
        }



        ///======================================================================================================================================================



        [SerializeField, Min(1)]
        [HideInInspector]
        private int width = 1;



        [SerializeField, Min(1)]
        [HideInInspector]
        private int height = 1;




        /// <summary>
        /// 2?차원 배열의 너비
        /// </summary>
        [FoldoutGroup("리니어그리드2D")]
        [HorizontalGroup("리니어그리드2D/너비높이")]
        [Sirenix.OdinInspector.ReadOnly]
        [ShowInInspector]
        [LabelText("너비")]
        [LabelWidth(40)]
        [PropertyOrder(0)]
        [DisplayAsString, EnableGUI]
        public int Width => width;



        /// <summary>
        /// 2?차원 배열의 높이
        /// </summary>
        [FoldoutGroup("리니어그리드2D")]
        [HorizontalGroup("리니어그리드2D/너비높이")]
        [Sirenix.OdinInspector.ReadOnly]
        [ShowInInspector]
        [LabelText("높이")]
        [LabelWidth(40)]
        [PropertyOrder(0)]
        [DisplayAsString, EnableGUI]
        public int Height => height;



        /// <summary>
        /// 2?차원 배열의 총 크기
        /// </summary>
        [FoldoutGroup("리니어그리드2D")]
        [HorizontalGroup("리니어그리드2D/너비높이")]
        [Sirenix.OdinInspector.ReadOnly]
        [ShowInInspector]
        [LabelText("총 크기")]
        [LabelWidth(50)]
        [PropertyOrder(0)]
        [DisplayAsString, EnableGUI]
        public int Length => cells != null ? cells.Length : -1;



        ///======================================================================================================================================================



        [FoldoutGroup("리니어그리드2D")]
        [SerializeField]
        [LabelText("셀 배열")]
        [PropertyOrder(1)]
        [ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false, ShowPaging = true, NumberOfItemsPerPage = 5)]
        private T[] cells = new T[1];



        public T[] GetCells => cells;



        ///======================================================================================================================================================



        private bool isValid;
        public bool IsValid => isValid;



        ///======================================================================================================================================================



        /// <summary>
        /// (x, y) 좌표의 셀에 접근합니다.
        /// </summary>
        public T this[int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => cells[ToIndex(x, y)];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => cells[ToIndex(x, y)] = value;
        }



        //? (x, y) → 1차원 인덱스 변환 (row-major)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ToIndex(int x, int y)
        {
#if UNITY_EDITOR
            //! 에디터 전용 범위 체크 – 잘못된 인덱스 접근 시 빠르게 발견
            if ((uint)x >= width || (uint)y >= height)
                throw new IndexOutOfRangeException($"({x},{y}) ∉ [0,{width - 1}]×[0,{height - 1}]");
#endif
            return x + y * width;
        }



        ///======================================================================================================================================================



        #region ▶ Odin Inspector 전용 디버그/조회 유틸
#if UNITY_EDITOR
        //? 조회할 좌표 입력 필드
        [FoldoutGroup("리니어그리드2D/셀 조회")]
        [HorizontalGroup("리니어그리드2D/셀 조회/셀조회가로", 0.3f)]
        [ShowIf(nameof(isValid))]
        [LabelText("X"), MinValue(0)]
        [LabelWidth(15)]
        [SerializeField]
        [PropertyOrder(9000)]
        private int debugX;

        [FoldoutGroup("리니어그리드2D/셀 조회")]
        [HorizontalGroup("리니어그리드2D/셀 조회/셀조회가로", 0.3f)]
        [ShowIf(nameof(isValid))]
        [LabelText("Y"), MinValue(0)]
        [LabelWidth(15)]
        [SerializeField]
        [PropertyOrder(9000)]
        private int debugY;

        //? 버튼: 좌표에 해당하는 셀 값을 프리뷰 필드에 저장
        [FoldoutGroup("리니어그리드2D/셀 조회")]
        [HorizontalGroup("리니어그리드2D/셀 조회/셀조회가로", 0.6f)]
        [ShowIf(nameof(isValid))]
        [PropertyOrder(9000)]
        [Button(" 셀 조회 ", ButtonAlignment = 0f, Stretch = false)]
        private void DebugShowCell()
        {
            if ((uint)debugX >= width || (uint)debugY >= height)
            {
                previewCell = default;
                previewValid = false;                //! 범위를 벗어나면 프리뷰 비활성화
                return;
            }
            previewCell = this[debugX, debugY];      //? 값 복사
            previewValid = true;
        }

        //? 조회된 셀 값 표시 – 읽기 전용
        [FoldoutGroup("리니어그리드2D/셀 조회"), ShowIf(nameof(previewValid))]
        [ShowIf(nameof(isValid))]
        [ShowInInspector, ReadOnlyCustom, LabelText("프리뷰 값"), InlineProperty, HideReferenceObjectPicker]
        [PropertyOrder(9001)]
        private T previewCell;

        //. 버튼 클릭 이후 유효 여부 플래그 (직렬화 불필요)
        [NonSerialized, HideInInspector]
        private bool previewValid;
#endif
        #endregion



        ///==================================================================================================================
    }



    ///======================================================================================================================================================
}