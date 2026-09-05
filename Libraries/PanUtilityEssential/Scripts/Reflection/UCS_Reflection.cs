using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;



namespace Pan.Util
{
    /// <summary>
    /// 유니티 엔진(또는 일반 C# 환경)에서 리플렉션을 보다 편리하게 사용하기 위한 확장 메서드를 모아둔 정적 클래스입니다.
    /// </summary>
    public static class SU_Reflection
    {
        ///======================================================================================================================================================

        /// <summary>
        /// 어떤 멤버를 대상으로 가져올지 결정하기 위한 열거형입니다.
        /// </summary>
        public enum MemberType
        {
            /// <summary>
            /// 필드만 가져옴
            /// </summary>
            Fields,
            /// <summary>
            /// 프로퍼티만 가져옴
            /// </summary>
            Properties,
            /// <summary>
            /// 메서드만 가져옴
            /// </summary>
            Methods,
            /// <summary>
            /// 필드, 프로퍼티, 메서드를 전부 가져옴
            /// </summary>
            All
        }

        /// <summary>
        /// 지정된 객체와/또는 해당 객체의 전역(static) 멤버를 Reflection으로 조회하여,
        /// <typeparamref name="T"/> 타입(또는 해당 타입을 상속하는 멤버)의 원본 이름과 값(또는 메서드 정보)을 가져옵니다.
        /// <list type="bullet">
        /// <item><description><c>T == object</c>인 경우: 필드/프로퍼티/메서드 전부, 별도 타입 매칭 없이 그대로 조회</description></item>
        /// <item><description><c>T == MethodInfo</c>인 경우: <see cref="MethodInfo"/>만 조회</description></item>
        /// <item><description><c>T</c>가 기타 다른 형식(예: <c>int</c>)이면, 해당 형식(또는 하위 클래스)으로 선언된 필드/프로퍼티/메서드만 필터링하여 조회</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">
        /// - 필드/프로퍼티/메서드의 실제 데이터 타입 혹은 <see cref="MethodInfo"/> 또는 <c>object</c>.
        /// </typeparam>
        /// <param name="target">
        /// Reflection 대상 객체. null이라도 인자로 받을 수 있음.
        /// null이면 인스턴스 멤버는 조회되지 않으며, <c>typeof(T)</c>를 기준으로 Reflection을 진행.
        /// </param>
        /// <param name="includeStaticMembers">
        /// true면 전역(static) 멤버도 가져옵니다. target이 null이든 아니든, <c>objectType</c>에서 static 멤버를 조회합니다.
        /// </param>
        /// <param name="includeNonPublic">true면 private/internal 등의 비공개 멤버도 포함</param>
        /// <param name="includeInherited">true면 부모 타입으로부터 상속받은 멤버도 포함</param>
        /// <param name="includeDerivedTypes">true면 T의 서브 클래스 타입도 T로 간주하여 필터링에 포함</param>
        /// <param name="memberTypes">가져올 멤버 유형(Fields/Properties/Methods/All 등)을 가변 인수로 지정</param>
        /// <returns>(string Name, T Value)의 튜플 리스트</returns>
        public static List<(string Name, T Value)> GetMembersWithOriginalNames<T>(
            object target,
            bool includeStaticMembers = false,
            bool includeNonPublic = false,
            bool includeInherited = false,
            bool includeDerivedTypes = true,
            params MemberType[] memberTypes)
        {
            //. Reflection 대상을 결정하기 위한 objectType
            //. target이 null이면 typeof(T) 기준, 아니면 target.GetType() 기준으로 진행
            Type objectType = (target != null)
                ? target.GetType()
                : typeof(T);

            //. BindingFlags 기본값은 Public으로 시작
            BindingFlags flags = BindingFlags.Public;

            //. static 멤버를 포함할지 여부
            if (includeStaticMembers)
            {
                flags |= BindingFlags.Static;
            }

            //. target이 null이 아니면 인스턴스 멤버 포함
            if (target != null)
            {
                flags |= BindingFlags.Instance;
            }

            //. 비공개 멤버 포함 여부
            if (includeNonPublic)
            {
                flags |= BindingFlags.NonPublic;
            }

            //. 상속받은 멤버를 제외하려면 DeclaredOnly
            if (!includeInherited)
            {
                flags |= BindingFlags.DeclaredOnly;
            }

            //. 멤버 유형 지정이 없으면 기본으로 All 처리
            if (memberTypes == null || memberTypes.Length == 0)
            {
                memberTypes = new[] { MemberType.All };
            }

            //. 결과를 담을 리스트
            var result = new List<(string, T)>();

            //. 중복을 제거하기 위한 HashSet
            //. 필드는 (정리된이름, FieldType)
            //. 프로퍼티는 (정리된이름, PropertyType)
            //. 메서드는 signature(string) 기반
            var seenFields = new HashSet<(string, Type)>();
            var seenProperties = new HashSet<(string, Type)>();
            var seenMethods = new HashSet<string>();

            //. 만약 All이 들어 있다면 나머지를 체크할 필요 없이 전부 가져옴
            bool getAll = Array.IndexOf(memberTypes, MemberType.All) >= 0;

            //. Fields, Properties, Methods 각각을 가져올지 여부
            bool getFields = getAll || Array.IndexOf(memberTypes, MemberType.Fields) >= 0;
            bool getProperties = getAll || Array.IndexOf(memberTypes, MemberType.Properties) >= 0;
            bool getMethods = getAll || Array.IndexOf(memberTypes, MemberType.Methods) >= 0;

            //? 1) 필드 가져오기
            if (getFields)
            {
                foreach (var field in objectType.GetFields(flags))
                {
                    if (field.IsDefined(typeof(CompilerGeneratedAttribute), true))
                        continue;

                    // 추가: 자동 구현 프로퍼티의 백잉 필드라면 건너뛰기
                    if (field.Name.StartsWith("<") && field.Name.EndsWith(">k__BackingField"))
                        continue;
         

                    //? T == object라면 모든 필드를 추가
                    //? 그렇지 않다면 field.FieldType과 typeof(T)가 같아야 추가
                    //? includeDerivedTypes가 true라면 field.FieldType이 T를 상속하는지 여부도 체크
                    if (typeof(T) == typeof(object)
                        || field.FieldType == typeof(T)
                        || (includeDerivedTypes && field.FieldType.IsSubclassOf(typeof(T))))
                    {
                        //. 백잉필드 이름 정리 + 인터페이스 구분자 제거
                        string nameCleaned = CleanFieldName(field);

                        //? 중복 체크 key
                        var fieldKey = (nameCleaned, field.FieldType);
                        if (!seenFields.Add(fieldKey))
                        {
                            // 이미 추가된 필드라면 스킵
                            continue;
                        }

                        //. static 필드 혹은 인스턴스 필드를 구분해서 GetValue
                        object fieldVal = field.GetValue(
                            field.IsStatic ? null : target
                        );

                        //. (string, T) 튜플로 변환
                        result.Add((nameCleaned, (T)(object)fieldVal));
                    }
                }
            }

            //? 2) 프로퍼티 가져오기
            if (getProperties)
            {
                foreach (var prop in objectType.GetProperties(flags))
                {
                    //. 읽기 가능한 프로퍼티만 처리
                    if (!prop.CanRead) continue;

                    //? T == object라면 전부, 아니면 prop.PropertyType == typeof(T)인 것만
                    //? includeDerivedTypes가 true라면 prop.PropertyType이 T를 상속하는지 여부도 체크
                    if (typeof(T) == typeof(object)
                        || prop.PropertyType == typeof(T)
                        || (includeDerivedTypes && prop.PropertyType.IsSubclassOf(typeof(T))))
                    {
                        //? 인터페이스 접두어 등 제거한 실제 프로퍼티 이름
                        string nameCleaned = CleanPropertyName(prop);

                        var propKey = (nameCleaned, prop.PropertyType);
                        if (!seenProperties.Add(propKey))
                        {
                            // 이미 추가된 프로퍼티라면 스킵
                            continue;
                        }

                        //. static 프로퍼티 or 인스턴스 프로퍼티 구분
                        object propVal = prop.GetValue(
                            prop.GetMethod.IsStatic ? null : target
                        );

                        result.Add((nameCleaned, (T)(object)propVal));
                    }
                }
            }

            //? 3) 메서드 가져오기
            if (getMethods)
            {
                foreach (var method in objectType.GetMethods(flags))
                {
                    //. 생성자, 속성 setter/getter 등 특수명은 제외
                    if (method.IsSpecialName)
                        continue;

                    //? 메서드의 반환형이 T(또는 T를 상속)인지 체크
                    bool returnTypeMatches = method.ReturnType == typeof(T) || (includeDerivedTypes && method.ReturnType.IsSubclassOf(typeof(T)));
                    bool isMethodInfoType = (typeof(T) == typeof(MethodInfo) || typeof(T) == typeof(object));

                    //? 시그니처 문자열 구성
                    string methodSignature = BuildMethodSignature(method);

                    // 중복 체크
                    if (!seenMethods.Add(methodSignature))
                    {
                        // 이미 추가된 메서드라면 스킵
                        continue;
                    }

                    //? T가 MethodInfo나 object인 경우: MethodInfo를 그대로 추가
                    if (isMethodInfoType)
                    {
                        result.Add((CleanMethodName(method), (T)(object)method));
                    }
                    else if (returnTypeMatches)
                    {
                        // 파라미터가 없는 메서드라면 실제로 호출해서 반환값을 추가
                        if (method.GetParameters().Length == 0)
                        {
                            object returnValue = method.Invoke(
                                method.IsStatic ? null : target,
                                null
                            );
                            result.Add((CleanMethodName(method), (T)returnValue));
                        }
                        else
                        {
                            // 파라미터가 있으면 호출 불가능. 이름만 추가하고 값은 default
                            result.Add((CleanMethodName(method), default(T)));
                        }
                    }
                }
            }

            return result;
        }



        /// <summary>
        /// 자동 구현 프로퍼티(Backing Field)의 이름을 실제 필드/프로퍼티명으로 변환하는 내부 메서드입니다.
        /// 추가로, 인터페이스 네임스페이스가 붙은 경우("ITest.Test")를 정리하기 위한 로직.
        /// </summary>
        private static string CleanFieldName(FieldInfo field)
        {
            string fieldName = field.Name;

            //. 자동 구현 프로퍼티의 백잉필드는 <PropertyName>k__BackingField 형태
            if (fieldName.StartsWith("<") && fieldName.EndsWith(">k__BackingField"))
            {
                fieldName = fieldName.Substring(1, fieldName.Length - 17);
            }

            //. 인터페이스에서 구현된 필드 등(거의 없겠지만) 점(.)으로 구분된 경우가 있을 수 있음
            int idx = fieldName.LastIndexOf('.');
            if (idx >= 0 && idx < fieldName.Length - 1)
            {
                fieldName = fieldName.Substring(idx + 1);
            }

            return fieldName;
        }

        /// <summary>
        /// 프로퍼티 이름에서 인터페이스 구분("ITest.Test") 등 점(.) 앞부분을 제거.
        /// </summary>
        private static string CleanPropertyName(PropertyInfo prop)
        {
            string name = prop.Name;

            // 인터페이스 명이 붙는 경우 예: "ITest.Test" -> "Test"
            int idx = name.LastIndexOf('.');
            if (idx >= 0 && idx < name.Length - 1)
            {
                name = name.Substring(idx + 1);
            }

            return name;
        }

        /// <summary>
        /// 메서드 이름에서 인터페이스 구분("ITest.Method") 등 점(.) 앞부분을 제거.
        /// </summary>
        private static string CleanMethodName(MethodInfo method)
        {
            string name = method.Name;
            int idx = name.LastIndexOf('.');
            if (idx >= 0 && idx < name.Length - 1)
            {
                name = name.Substring(idx + 1);
            }
            return name;
        }

        /// <summary>
        /// MethodInfo에서 중복 체크를 위한 시그니처 문자열을 만드는 내부 메서드입니다.
        /// 인터페이스 구분자를 제거하여, 실제 이름만 남깁니다.
        /// </summary>
        private static string BuildMethodSignature(MethodInfo method)
        {
            var paramTypes = method.GetParameters()
                .Select(p => p.ParameterType.FullName);
            string paramPart = string.Join(",", paramTypes);

            //. 메서드 네임도 정리.
            string cleanedName = CleanMethodName(method);
            return $"{cleanedName}({paramPart}):{method.ReturnType.FullName}";
        }

        /// <summary>
        /// 지정된 객체에서 주어진 메서드 이름으로 메서드를 찾고, 해당 메서드를 호출하여 그 결과를 반환합니다.
        /// </summary>
        /// <param name="target">메서드를 찾을 대상 객체</param>
        /// <param name="methodName">찾고자 하는 메서드의 이름</param>
        /// <param name="parameters">메서드에 전달할 매개변수들</param>
        /// <returns>메서드 호출 결과 (반환값이 없으면 null)</returns>
        public static object InvokeMethodByName(object target, string methodName, params object[] parameters)
        {
            if (target == null)
            {
                //! target이 null이면 메서드를 찾을 수 없음
                throw new ArgumentNullException(nameof(target));
            }

            //. 해당 이름의 메서드 정보를 BindingFlags로 검색
            var t = target.GetType();
            MethodInfo mi = t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (mi == null)
            {
                //! 메서드를 발견할 수 없을 경우 예외 발생
                throw new MissingMethodException($"메서드를 찾을 수 없습니다: {methodName}");
            }

            return mi.Invoke(target, parameters);
        }

        /// <summary>
        /// 문자열 형태의 전체 타입 이름(네임스페이스 포함)으로부터 해당 타입을 찾아 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="fullTypeName">예: "System.Text.StringBuilder"와 같은 전체 타입 이름</param>
        /// <param name="args">생성자 매개변수들</param>
        /// <returns>생성된 인스턴스 객체</returns>
        public static object CreateInstanceByTypeName(string fullTypeName, params object[] args)
        {
            //. Type.GetType으로 해당 타입 정보를 가져옴
            var type = Type.GetType(fullTypeName);
            if (type == null)
            {
                //! 타입을 찾을 수 없는 경우 예외 처리
                throw new ArgumentException($"해당 이름의 타입을 찾을 수 없습니다: {fullTypeName}");
            }

            //. Activator를 이용해 인스턴스를 생성
            return Activator.CreateInstance(type, args);
        }

        ///======================================================================================================================================================

        /// <summary>
        /// 특정 객체가 직접 구현한 모든 인터페이스를 반환합니다.
        /// </summary>
        /// <param name="obj">인터페이스를 검사할 객체</param>
        /// <param name="includeInherited">부모 클래스로부터 상속받은 인터페이스도 포함할지 여부</param>
        /// <returns>해당 객체가 구현한 모든 인터페이스 목록</returns>
        public static IEnumerable<Type> GetImplementedInterfaces(object obj, bool includeInherited = true)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            Type type = obj.GetType();
            return includeInherited ? type.GetInterfaces() : type.GetTypeInfo().ImplementedInterfaces;
        }

        /// <summary>
        /// 특정 객체가 상속받고 있는 인터페이스 중 특정 인터페이스를 상속하는 인터페이스만 반환합니다.
        /// </summary>
        /// <typeparam name="TBaseInterface">필터링할 기준 인터페이스</typeparam>
        /// <param name="obj">인터페이스를 검사할 객체</param>
        /// <param name="includeInherited">부모 클래스로부터 상속받은 인터페이스도 포함할지 여부</param>
        /// <returns>특정 인터페이스를 상속하는 인터페이스 목록</returns>
        public static IEnumerable<Type> GetImplementedInterfacesOfType<TBaseInterface>(object obj, bool includeInherited = true)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            return GetImplementedInterfaces(obj, includeInherited)
                .Where(i => typeof(TBaseInterface).IsAssignableFrom(i));
        }

        /// <summary>
        /// 특정 객체가 상속받고 있는 인터페이스 중 특정 인터페이스를 상속하는 인터페이스만 반환합니다.
        /// </summary>
        /// <param name="obj">인터페이스를 검사할 객체</param>
        /// <param name="baseInterfaceType">필터링할 기준 인터페이스 타입</param>
        /// <param name="includeInherited">부모 클래스로부터 상속받은 인터페이스도 포함할지 여부</param>
        /// <returns>특정 인터페이스를 상속하는 인터페이스 목록</returns>
        public static IEnumerable<Type> GetImplementedInterfacesOfType(object obj, Type baseInterfaceType, bool includeInherited = true)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (baseInterfaceType == null) throw new ArgumentNullException(nameof(baseInterfaceType));
            if (!baseInterfaceType.IsInterface) throw new ArgumentException("baseInterfaceType은 반드시 인터페이스여야 합니다.", nameof(baseInterfaceType));

            return GetImplementedInterfaces(obj, includeInherited)
                .Where(i => baseInterfaceType.IsAssignableFrom(i));
        }

        ///======================================================================================================================================================
    }
}
