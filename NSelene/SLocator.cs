﻿using System;
using System.Collections.Generic;
using System.Drawing;
using NSelene.Conditions;
using OpenQA.Selenium;
using System.Linq;
using System.Collections.ObjectModel;

namespace NSelene
{

    public abstract class SLocator<TEntity>
    {

        public abstract string Description { get; }
        public abstract TEntity Find();

        public override string ToString ()
        {
            return Description;
        }
    }

    public abstract class WebElementSLocator : SLocator<IWebElement>
    {
    }

    public sealed class DriverWebElementSLocator : WebElementSLocator
    {
        readonly DriverProvider engine;
        readonly By driverLocator;

        public DriverWebElementSLocator(By driverLocator, DriverProvider engine)
        {
            this.driverLocator = driverLocator;
            this.engine = engine;
        }

        public override string Description {
            get {
                return this.driverLocator.ToString();
            }
        }

        public override IWebElement Find ()
        {
            return this.engine.Driver.FindElement(this.driverLocator); // TODO: it violates "Tell Don't Ask" principle... should we fix it?
        }
    }

    public sealed class WrappedWebElementSLocator : WebElementSLocator
    {
        readonly string description;
        readonly IWebElement webelement;

        public WrappedWebElementSLocator(string description, IWebElement webelement)
        {
            this.webelement = webelement;
            this.description = description;
        }

        public WrappedWebElementSLocator(IWebElement webelement)
            : this("wrapped webelement", webelement) {}

        public override string Description {
            get {
                return this.description + ": " + this.webelement;
            }
        }

        public override IWebElement Find ()
        {
            return this.webelement;
        }
    }

    // TODO: rename to SElementInnerWebElementSLocator ?
    public sealed class InnerWebElementSLocator : WebElementSLocator
    {
        readonly By driverLocator;
        readonly SElement context;

        public InnerWebElementSLocator(By driverLocator, SElement context)
        {
            this.driverLocator = driverLocator;
            this.context = context;
        }

        public override string Description {
            get {
                return string.Format("By.Selene: ({0}).FindInner({1})", this.context, driverLocator);
            }
        }

        public override IWebElement Find ()
        {
            return this.context.Should(Be.Visible).ActualWebElement.FindElement(this.driverLocator);
        }
    }

    public sealed class SCollectionWebElementByIndexSLocator : WebElementSLocator
    {
        readonly int index;
        readonly SCollection context;

        public SCollectionWebElementByIndexSLocator(int index, SCollection context)
        {
            this.index = index;
            this.context = context;
        }

        public override string Description {
            get {
                return string.Format("By.Selene: ({0})[{1}]", this.context, index);
            }
        }

        public override IWebElement Find ()
        {
            return this.context.Should(Have.CountAtLeast(this.index+1)).ActualWebElements.ElementAt(index);
        }
    }

    public sealed class SCollectionWebElementByConditionSLocator : WebElementSLocator
    {
        readonly Condition<SElement> condition;
        readonly SCollection context;
        readonly DriverProvider engine;

        public SCollectionWebElementByConditionSLocator(Condition<SElement> condition, SCollection context, DriverProvider engine)
        {
            this.condition = condition;
            this.context = context;
            this.engine = engine;
        }

        public override string Description {
            get {
                return string.Format("By.Selene: ({0}).FindBy({1})"
                                     , this.context
                                     , this.condition.Explain());
            }
        }

        public override IWebElement Find ()
        {
            var webelments = this.context.ActualWebElements;

            Predicate<IWebElement> byCondition = delegate(IWebElement element) {
                return condition.Apply(
                    new SElement(
                        new WrappedWebElementSLocator(
                            string.Format("By.Selene: ({0}).FindBy({1})", this.context, condition.Explain())
                            , element), this.engine));/* 
                            * ??? TODO: do we actually need here so meaningful desctiption?
                            * does it make sense to use it but to put index for each element?
                            * via using FindIndex ?
                            */
            };

            var found = webelments.ToList()
                                  .Find(byCondition);
            if (found == null) 
            {
                var actualTexts = webelments.ToList().Select(element => element.Text).ToArray();
                var htmlelements = webelments.ToList().Select(element => element.GetAttribute("outerHTML")).ToArray();
                throw new NotFoundException("element was not found in collection by condition "
                                            + condition.Explain()
                                            + "\n  Actual visible texts : " + "[" + string.Join(",", actualTexts) + "]"  // TODO: think: this line is actually needed in the case of FindBy(ExactText ...) ... is there any way to add such information not here?
                                            + "\n  Actual html elements : " + "[" + string.Join(",", htmlelements) + "]" 
                                            // TODO: should we add here some other info about elements? e.g. visiblitiy?
                                           );
                /*
                     * TODO: think on better messages
                     */
            }
            return found;
        }
    }

    public abstract class WebElementsCollectionSLocator : SLocator<IReadOnlyCollection<IWebElement>>
    {
    }

    public sealed class DriverWebElementsCollectionSLocator : WebElementsCollectionSLocator
    {
        readonly DriverProvider engine;
        readonly By driverLocator;

        public DriverWebElementsCollectionSLocator(By driverLocator, DriverProvider engine)
        {
            this.driverLocator = driverLocator;
            this.engine = engine;
        }

        public override string Description {
            get {
                return this.driverLocator.ToString();
            }
        }

        public override IReadOnlyCollection<IWebElement> Find ()
        {
            return this.engine.Driver.FindElements(this.driverLocator);
        }
    }

    public sealed class WrappedWebElementsCollectionSLocator : WebElementsCollectionSLocator
    {
        readonly string description;
        readonly IList<IWebElement> webelements;

        public WrappedWebElementsCollectionSLocator(string description, IList<IWebElement> webelements)
        {
            this.webelements = webelements;
            this.description = description;
        }

        public WrappedWebElementsCollectionSLocator(IList<IWebElement> webelements) 
            : this("wrapped collection", webelements) {}  // TODO: think on better description

        public override string Description {
            get {
                return this.description + ": " + webelements.ToString();
            }
        }

        public override IReadOnlyCollection<IWebElement> Find()
        {
            return new ReadOnlyCollection<IWebElement>(this.webelements);  // TODO: think on switching SCollection impl to be based on IList<IWebElement>
        }
    }

    public sealed class InnerWebElementsCollectionSLocator : WebElementsCollectionSLocator
    {
        readonly By driverLocator;
        readonly SElement context;

        public InnerWebElementsCollectionSLocator(By driverLocator, SElement context)
        {
            this.driverLocator = driverLocator;
            this.context = context;
        }

        public override string Description {
            get {
                return string.Format("By.Selene: ({0}).FindAllInner({1})", this.context, driverLocator);
            }
        }

        public override IReadOnlyCollection<IWebElement> Find ()
        {
            return this.context.Should(Be.Visible).ActualWebElement.FindElements(this.driverLocator);
        }
    }

    public sealed class SCollectionFilteredWebElementsCollectionSLocator : WebElementsCollectionSLocator
    {
        readonly Condition<SElement> condition;
        readonly SCollection context;
        readonly DriverProvider engine;

        public SCollectionFilteredWebElementsCollectionSLocator(Condition<SElement> condition, SCollection context, DriverProvider engine)
        {
            this.condition = condition;
            this.context = context;
            this.engine = engine;
        }

        public override string Description {
            get {
                return string.Format("By.Selene: ({0}).FilterBy({1})", this.context, this.condition.Explain());
            }
        }

        public override IReadOnlyCollection<IWebElement> Find ()
        {

            Func<IWebElement, bool> byCondition = delegate(IWebElement element) {
                return condition.Apply(
                    new SElement(
                        new WrappedWebElementSLocator(
                            string.Format("By.Selene: ({0}).FindBy({1})"
                                          , this.context
                                          , this.condition.Explain())
                            , element), this.engine));
            };

            return new ReadOnlyCollection<IWebElement>(  // TODO: don't we need here ReadONlyCollection<SElement> ?
                context.ActualWebElements.Where(byCondition).ToList());
        }
    }
}

