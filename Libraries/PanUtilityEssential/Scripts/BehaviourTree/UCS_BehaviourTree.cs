using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Pan.Util;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using UnityEngine.PlayerLoop;



//? 직접 만든 수제 행동 트리 (Behaviour Tree) 가 있는 정도의 코드



namespace Pan.Util.PanTree
{
    public enum ETreeStatus
    {
        Success, Failure, Running
    }



    public enum ENodeType
    {
        Sequence, Selector
    }



    public interface INode
    {
        ETreeStatus Invoke();
    }



    public interface ICompositeNode
    {
        Queue<INode> ChildsQueue { get; }
    }



    public interface IDecorator
    {
        Func<bool> Decorator { get; }
    }



    public interface ITask
    {
        Func<ETreeStatus> TaskAction { get; }
    }



    public abstract class BaseNode : INode
    {
        public string Name;



        public abstract ETreeStatus Invoke();
    }



    public abstract class Node : BaseNode, INode
    {
        public CompositeNode Parent { get; set; }



        public CompositeNode End() { return Parent; }
    }



    public abstract class CompositeNode : Node, ICompositeNode
    {
        Queue<INode> ICompositeNode.ChildsQueue => ChildsQueue;
        protected readonly Queue<INode> ChildsQueue = new Queue<INode>();


        public TNode AddChild<TNode>(TNode node) where TNode : Node
        {
            ChildsQueue.Enqueue(node);

            node.Parent = this;

            return node;
        }



        public SelecorNode SL()
        {
            return AddChild(new SelecorNode());
        }



        public SequenceNode SQ()
        {
            return AddChild(new SequenceNode());
        }



        public DecoratorNode Deco(Func<bool> decorator, ENodeType nodeType)
        {
            return AddChild(new DecoratorNode(decorator, nodeType));
        }



        public DecoratorNode Deco_SL(Func<bool> decorator)
        {
            return AddChild(new DecoratorNode(decorator, ENodeType.Selector));
        }



        public DecoratorNode Deco_SQ(Func<bool> decorator)
        {
            return AddChild(new DecoratorNode(decorator, ENodeType.Sequence));
        }



        public CompositeNode Task(Func<ETreeStatus> taskAction)
        {
            AddChild(new Task(taskAction));
            return this;
        }



        public CompositeNode Task(Action successTaskAction)
        {
            AddChild(new Task(successTaskAction));
            return this;
        }


        public CompositeNode Task_Deco(Func<bool> decorator, Action successTaskAction)
        {
            AddChild(new DecoratorTask(decorator, successTaskAction));
            return this;
        }


        public CompositeNode Task(ETreeStatus result, Action customTaskAction)
        {
            AddChild(new Task(result, customTaskAction));
            return this;
        }



        public CompositeNode Task_Twins(Func<bool> condition, Func<ETreeStatus> trueTask, Func<ETreeStatus> falseTask)
        {
            AddChild(new TaskTwins(condition, trueTask, falseTask));
            return this;
        }




        public CompositeNode Task_Twins(Func<bool> condition, Action trueSuccessTask, Action falseSuccessTask)
        {
            AddChild(new TaskTwins(condition, trueSuccessTask, falseSuccessTask));
            return this;
        }



        public CompositeNode Task_Twins(ETreeStatus result, Func<bool> condition, Action trueSuccessTask, Action falseSuccessTask)
        {
            AddChild(new TaskTwins(result, condition, trueSuccessTask, falseSuccessTask));
            return this;
        }



        public CompositeNode Task_Twins(ETreeStatus resultTrue, ETreeStatus resultFalse, Func<bool> condition, Action trueSuccessTask, Action falseSuccessTask)
        {
            AddChild(new TaskTwins(resultTrue, resultFalse, condition, trueSuccessTask, falseSuccessTask));
            return this;
        }



        /// <summary>
        /// 셀렉터 : 자식 노드들 중에서 딱 하나만 실행시키기 위한 노드<br/>
        /// 순회중, 자식 노드들중 하나라도 Success 또는 Running 을 반환할 경우,<br/>
        /// 즉시 순회를 중단하고, 부모 노드에 Success 또는 Running 을 반환한다<br/>
        /// 순회중, 모두 Failure를 반환했을 경우 부모 노드에 Failure를 반환한다<br/>
        /// </summary>
        public static ETreeStatus Invoke_Selector(ICompositeNode node)
        {

            foreach (var child in node.ChildsQueue)
            {
                var result = child.Invoke();

                //? 순회도중 Success 또는 Running 일 경우,
                //? 순회를 중단하고 즉시 return 한다
                if (result == ETreeStatus.Success || result == ETreeStatus.Running)
                {
                    return result;
                }
            }

            //? 순회를 전부 했지만 Success나 Running이 나오지 않았으므로
            //? Failure를 반환한다
            return ETreeStatus.Failure;
        }


        /// <summary>
        /// 시퀀스 : 자식 노드들을 순서대로 실행 하기 위한 노드<br/>
        /// 순회중, 자식 노드들중 하나라도 Failure를 반환할 경우,<br/>
        /// 즉시 순회를 중단하고, 부모 노드에 Failure를 반환한다<br/>
        /// 순회중, 모두 Success를 반환했을 경우 부모 노드에 Success를 반환한다<br/>
        /// </summary>
        public static ETreeStatus Invoke_Sequence(ICompositeNode node)
        {
            foreach (var child in node.ChildsQueue)
            {
                var result = child.Invoke();


                //? 순회 도중 Failure 일경우,
                //? 순회를 중단하고 즉시 return 한다
                if (result == ETreeStatus.Failure) { return ETreeStatus.Failure; }
            }

            //? 순회를 전부 Succes 할경우 Success를 반환한다
            return ETreeStatus.Success;
        }
    }



    public class SelecorNode : CompositeNode
    {
        public override ETreeStatus Invoke() => Invoke_Selector(this);
    }



    public class SequenceNode : CompositeNode
    {
        public override ETreeStatus Invoke() => Invoke_Sequence(this);
    }



    public class DecoratorNode : CompositeNode, IDecorator
    {
        public DecoratorNode(Func<bool> decorator, ENodeType nodeType)
        {
            this.decorator = decorator;
            NodeType = nodeType;
        }



        public Func<bool> Decorator => decorator;
        private readonly Func<bool> decorator;



        public readonly ENodeType NodeType;



        public override ETreeStatus Invoke()
        {
            switch (decorator.Invoke())
            {
                case true:

                switch (NodeType)
                {
                    case ENodeType.Sequence: return Invoke_Sequence(this);
                    case ENodeType.Selector: return Invoke_Selector(this);
                }

                break;
            }

            return ETreeStatus.Failure;
        }
    }



    public class DecoratorSelector : DecoratorNode
    {
        public DecoratorSelector(Func<bool> decorator) : base(decorator, ENodeType.Selector) { }
    }



    public class DecoratorSequence : DecoratorNode
    {
        public DecoratorSequence(Func<bool> decorator) : base(decorator, ENodeType.Sequence) { }
    }



    public abstract class BaseTaskNode : Node
    {

    }



    public class Task : BaseTaskNode, ITask
    {



        /// <summary>작업 노드 만들기</summary>
        public Task(Func<ETreeStatus> taskAction)
        {
            this.taskAction = taskAction;
        }



        /// <summary>무조건 Success만 반환하는 작업 노드 만들기</summary>
        public Task(Action successTaskAction)
        {
            taskAction = () =>
            {
                successTaskAction?.Invoke();
                return ETreeStatus.Success;
            };
        }



        /// <summary>무조건 지정 Status만 반환하는 작업 노드 만들기</summary>
        public Task(ETreeStatus result, Action customTaskAction)
        {
            this.taskAction = () =>
            {
                customTaskAction?.Invoke();
                return result;
            };
        }



        public Func<ETreeStatus> TaskAction => taskAction;
        private readonly Func<ETreeStatus> taskAction;



        public override ETreeStatus Invoke() => taskAction.Invoke();
    }



    public class DecoratorTask : BaseTaskNode, ITask
    {



        /// <summary>조건 작업 노드 만들기</summary>
        public DecoratorTask(Func<bool> decorator, Action successTaskAction)
        {
            Decorator = decorator;

            taskAction = () =>
            {
                successTaskAction?.Invoke();
                return ETreeStatus.Success;
            };
        }



        private readonly Func<bool> Decorator;

        public Func<ETreeStatus> TaskAction => taskAction;
        private readonly Func<ETreeStatus> taskAction;



        public override ETreeStatus Invoke()
        {
            var decorator = Decorator.Invoke();

            if (decorator)
            {
                taskAction.Invoke(); //! 무조건 Success 반환
            }


            return ETreeStatus.Failure; //! 조건에 맞지않았으면 Failure
        }
    }



    public class TaskTwins : BaseTaskNode
    {



        /// <summary>쌍둥이 노드 만들기</summary>
        /// <param name="condition">구분지을 조건</param>
        /// <param name="trueTask">True시 작업</param>
        /// <param name="falseTask">Flase시 작업</param>
        public TaskTwins(Func<bool> condition, Func<ETreeStatus> trueTask, Func<ETreeStatus> falseTask)
        {
            Condition = condition;
            TrueTask = trueTask;
            FalseTask = falseTask;
        }



        /// <summary>무조건 Success만 반환하는 쌍둥이 노드 만들기</summary>
        /// <param name="condition">구분지을 조건</param>
        /// <param name="trueTask">True시 작업</param>
        /// <param name="falseTask">Flase시 작업</param>
        public TaskTwins(Func<bool> condition, Action trueSuccessTask, Action falseSuccessTask)
        {
            Condition = condition;
            TrueTask = () => { trueSuccessTask.Invoke(); return ETreeStatus.Success; };
            FalseTask = () => { falseSuccessTask.Invoke(); return ETreeStatus.Success; };
        }



        /// <summary>무조건 지정 Status만 반환하는 쌍둥이 노드 만들기</summary>
        /// <param name="condition">구분지을 조건</param>
        /// <param name="trueTask">True시 작업</param>
        /// <param name="falseTask">Flase시 작업</param>
        public TaskTwins(ETreeStatus result, Func<bool> condition, Action trueSuccessTask, Action falseSuccessTask)
        {
            Condition = condition;
            TrueTask = () => { trueSuccessTask.Invoke(); return result; };
            FalseTask = () => { falseSuccessTask.Invoke(); return result; };
        }



        /// <summary>무조건 지정 Status들만 반환하는 쌍둥이 노드 만들기</summary>
        /// <param name="condition">구분지을 조건</param>
        /// <param name="trueTask">True시 작업</param>
        /// <param name="falseTask">Flase시 작업</param>
        public TaskTwins(ETreeStatus resultTrue, ETreeStatus resultFalse, Func<bool> condition, Action trueSuccessTask, Action falseSuccessTask)
        {
            Condition = condition;
            TrueTask = () => { trueSuccessTask.Invoke(); return resultTrue; };
            FalseTask = () => { falseSuccessTask.Invoke(); return resultFalse; };
        }



        private readonly Func<bool> Condition;
        private readonly Func<ETreeStatus> TrueTask;
        private readonly Func<ETreeStatus> FalseTask;



        public override ETreeStatus Invoke()
        {
            switch (Condition.Invoke())
            {
                case true: return TrueTask.Invoke();
                case false: return FalseTask.Invoke();
            }
        }



    }
}