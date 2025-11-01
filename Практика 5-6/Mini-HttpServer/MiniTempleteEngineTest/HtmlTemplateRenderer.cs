using MiniTemplateEngine;

namespace MiniTempleteEngineTest
{
    [TestClass]
    public sealed class HtmlTemplateRendererTest
    {
        [TestMethod]
        public void RenderFromFile_When_Return()
        {
            //Arange
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1> Привет, ${Name} </h1>";
            var model = new { Name = "Тимерхан" };
            string expectedString = "<h1> Привет, Тимерхан </h1>";

            //Act
            var result = testee.RenderFromString(templateHtml, model);

            //Assert
            Assert.AreEqual(expectedString, result);
        }

        [TestMethod]
        public void RenderFromFile_WhenDoubleReplace_ReturnCorrectString()
        {
            //Arange
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1> Привет, ${Name} </h1><p> Привет, ${Name} </p>";
            var model = new { Name = "Тимерхан", Email = "test@test.ru" };
            string expectedString = "<h1> Привет, Тимерхан </h1><p> Привет, Тимерхан </p>";

            //Act
            var result = testee.RenderFromString(templateHtml, model);

            //Assert
            Assert.AreEqual(expectedString, result);
        }

        [TestMethod]
        public void RenderFromFile_WhenTwoProperties_ReturnCorrectString()
        {
            //Arange
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1>Привет, ${Name}</h1><p>Привет, ${Email}</p>";
            var model = new { Name = "Тимерхан", Email = "test@test.ru" };
            string expectedString = "<h1>Привет, Тимерхан</h1><p>Привет, test@test.ru</p>";

            //Act
            var result = testee.RenderFromString(templateHtml, model);

            //Assert
            Assert.AreEqual(expectedString, result);
        }

        [TestMethod]
        public void RenderFromFile_SubProperties_ReturnCorrectString()
        {
            //Arange
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1>Привет, ${Name}</h1><p>группа: ${Group.Name}</p>";
            var model = new { Name = "Тимерхан", Email = "test@test.ru",
                Group = new
                {
                    Id = 1,
                    Name = "11-409",
                }
            };
            string expectedString = "<h1>Привет, Тимерхан</h1><p>группа: 11-409</p>";

            //Act
            var result = testee.RenderFromString(templateHtml, model);

            //Assert
            Assert.AreEqual(expectedString, result);
        }

        [TestMethod]
        public void RenderFromString_IfElse_ConditionIsNotNull()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();

            string templateHtml = "<h1> $if(Name) <p> Привет, Никита </p> $else <p> Привет, Анон</p> $endif </h1>";

            var model = new
            {
                Name = "Никита", 
            };

            string expectedString = "<h1> <p> Привет, Никита </p> </h1>";

            // Act
            var result = testee.RenderFromString(templateHtml, model);

            // Assert
            Assert.AreEqual(expectedString.Trim(), result.Trim()); 
        }

        // 01. Проверка переменной, которой нет (null)
        [TestMethod]
        public void RenderFromString_Variable_NullValue_ReturnsEmpty()
        {
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1>Привет, ${NonExistent}</h1>";
            var model = new { Name = "Тимерхан" };
            string expectedString = "<h1>Привет, </h1>";
            var result = testee.RenderFromString(templateHtml, model);
            Assert.AreEqual(expectedString, result);
        }

        // 02. Проверка числового значения
        [TestMethod]
        public void RenderFromString_Variable_NumericValue_ReturnsString()
        {
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "ID: ${Id}";
            var model = new { Id = 42 };
            string expectedString = "ID: 42";
            var result = testee.RenderFromString(templateHtml, model);
            Assert.AreEqual(expectedString, result);
        }

        // 03. Проверка пробелов вокруг пути переменной
        [TestMethod]
        public void RenderFromString_Variable_WithWhitespace_IsTrimmed()
        {
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "Имя: ${ Name }";
            var model = new { Name = "Тимерхан" };
            string expectedString = "Имя: Тимерхан";
            var result = testee.RenderFromString(templateHtml, model);
            Assert.AreEqual(expectedString, result);
        }

        // 04. Проверка $else, когда условие null
        [TestMethod]
        public void RenderFromString_IfElse_ConditionIsNull_UsesElseBlock()
        {
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1> $if(NonExistent) <p> Истина </p> $else <p> Ложь </p> $endif </h1>";
            var model = new { Name = "Тимерхан" }; // NonExistent будет null
            string expectedString = "<h1> <p> Ложь </p> </h1>";
            var result = testee.RenderFromString(templateHtml, model);
            Assert.AreEqual(expectedString, result);
        }

        // 05. Проверка чистого boolean со значением false (без $else)
        [TestMethod]
        public void RenderFromString_If_ConditionIsFalseBool_UsesEmptyString()
        {
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<p>$if(IsAdmin) Secret Content $endif</p>";
            var model = new { IsAdmin = false };
            string expectedString = "<p></p>";
            var result = testee.RenderFromString(templateHtml, model);
            Assert.AreEqual(expectedString, result);
        }

        // 06. Проверка boolean, заданного строкой "False"
        [TestMethod]
        public void RenderFromString_IfElse_ConditionIsFalseString_UsesElseBlock()
        {
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "Msg: $if(Status) ON $else OFF $endif";
            var model = new { Status = "False" };
            string expectedString = "Msg: OFF";
            var result = testee.RenderFromString(templateHtml, model);
            Assert.AreEqual(expectedString, result);
        }

        // 07. Проверка $if без $else, когда условие ложно (должно исчезнуть)
        [TestMethod]
        public void RenderFromString_If_NoElseBlock_ConditionIsFalse_ReturnsEmpty()
        {
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "Header: $if(IsActive) Content $endif";
            var model = new { IsActive = false };
            string expectedString = "Header:";
            var result = testee.RenderFromString(templateHtml, model);
            Assert.AreEqual(expectedString, result);
        }

        // 08. Проверка условия на вложенное свойство (Group.Id != null)
        [TestMethod]
        public void RenderFromString_If_NestedProperties_ConditionIsTrue()
        {
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "Статус: $if(Group.Id) Присвоена $else Не присвоена $endif";
            var model = new { Group = new { Id = 100 } };
            string expectedString = "Статус: Присвоена";
            var result = testee.RenderFromString(templateHtml, model);
            Assert.AreEqual(expectedString, result);
        }

        // 09. Проверка цикла по пустой коллекции
        [TestMethod]
        public void RenderFromString_Foreach_EmptyCollection_ReturnsEmpty()
        {
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<ul>$foreach(var item in Items)<li>${item.Name}</li>$endfor</ul>";
            var model = new { Items = new List<object>() };
            string expectedString = "<ul></ul>";
            var result = testee.RenderFromString(templateHtml, model);
            Assert.AreEqual(expectedString, result);
        }

        // 10. Проверка цикла по списку строк
        [TestMethod]
        public void RenderFromString_Foreach_SimpleIteration_ReturnsCorrectList() // не работает
        {
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "Names: $foreach(var n in Names)${n}; $endfor";
            var model = new { Names = new List<string> { "A", "B" } };
            string expectedString = "Names: A; B;";
            var result = testee.RenderFromString(templateHtml, model);
            Assert.AreEqual(expectedString, result);
        }

        // 11. Проверка цикла по коллекции объектов (доступ к свойству)
        [TestMethod]
        public void RenderFromString_Foreach_AccessSubProperty_ReturnsCorrectItems()
        {
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<ul>$foreach(var p in Products)<li>${Name}: ${Price}</li>$endfor</ul>";
            var model = new
            {
                Products = new List<object> {
            new { Name = "Книга", Price = 100 },
            new { Name = "Ручка", Price = 10 }
        }
            };
            string expectedString = "<ul><li>Книга: 100</li><li>Ручка: 10</li></ul>";
            var result = testee.RenderFromString(templateHtml, model);
            Assert.AreEqual(expectedString, result);
        }

        // 12. Проверка цикла по вложенной коллекции (используем рефлексию GetValueByPath)
        [TestMethod]
        public void RenderFromString_Foreach_NestedProperties_ReturnsCorrectItems()
        {
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "Посты: $foreach(var p in User.Posts) - ${p.Title}$endfor";
            var model = new
            {
                User = new
                {
                    Posts = new List<object> { new { Title = "1" }, new { Title = "2" } }
                }
            };
            string expectedString = "Посты: - 1 - 2";
            var result = testee.RenderFromString(templateHtml, model);
            Assert.AreEqual(expectedString, result);
        }

        // 13. Проверка вложенности: foreach внутри if
        [TestMethod]
        public void RenderFromString_Foreach_InsideIf_ReturnsCorrectItems() //
        {
            var testee = new HtmlTemplateRenderer();
            var model = new
            {
                Items = new List<object> {
                new { Value = "A" },
                new { Value = "B" }
                }
            };
            string templateHtml = "Names: $foreach(var item in Items) ${item.Value}; $endfor";
            string expectedString = "Names: A; B;";
            var result = testee.RenderFromString(templateHtml, model);
            Assert.AreEqual(expectedString, result);
        }

        // 14. Проверка вложенности: if внутри foreach
        [TestMethod]
        public void RenderFromString_If_InsideForeach_RendersConditionally() // не работает
        {
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "$foreach(var item in Items)$if(item.IsActive) - Active ${item.Name} $endif$endfor";
            var model = new
            {
                Items = new List<object> {
            new { Name = "A", IsActive = true },
            new { Name = "B", IsActive = false },
            new { Name = "C", IsActive = true }
        }
            };
            string expectedString = "- Active A - Active C";
            var result = testee.RenderFromString(templateHtml, model);
            Assert.AreEqual(expectedString, result);
        }

        // 15. Комплексный тест: foreach -> if -> variable (проверка полной рекурсии)
        [TestMethod]
        public void RenderFromString_Complex_AllFeaturesCombined() // не работает
        {
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "Users: $foreach(var u in Users) $if(u.Admin) ${u.Name}(ADMIN) $else${u.Name}$endif$endfor";
            var model = new
            {
                Users = new List<object> {
            new { Name = "Тима", Admin = true },
            new { Name = "Анон", Admin = false }
        }
            };
            string expectedString = "Users: Тима(ADMIN) Анон";
            var result = testee.RenderFromString(templateHtml, model);
            Assert.AreEqual(expectedString, result);
        }

        [TestMethod]
        public void RenderFromString_When_ComplexTemplate_RendersAllCorrectly()
        {
            // arrange
            HtmlTemplateRenderer renderer = new HtmlTemplateRenderer();

            string template = "<h1>${user.Name}'s Dashboard</h1>$if(user.IsActive)<p>Status: Active</p>$else<p>Status: Inactive</p>$endif<p>Age: ${user.Age}</p>$if(user.Orders)<h2>Orders:</h2><ul>$foreach(var order in user.Orders)<li><b>Order #${order.Id}</b> - ${order.Title}$if(order.IsPaid)<span>(Paid)</span>$else<span>(Pending)</span>$endif<ul>$foreach(var item in order.Items)<li>${item.Name} x ${item.Quantity}</li>$endfor</ul></li>$endfor</ul>$endif$if(user.Notifications)<h2>Notifications:</h2><ul>$foreach(var note in user.Notifications)<li>${note}</li>$endfor</ul>$endif$if(user.PromoCode)<p>Your promo code: ${user.PromoCode}</p>$endif<footer>Generated for ${user.Name}</footer>";


            var model = new
            {
                user = new
                {
                    Name = "Alex",
                    Age = 32,
                    IsActive = true,
                    Orders = new[]
                    {
                new
                {
                    Id = 101,
                    Title = "Electronics",
                    IsPaid = true,
                    Items = new[]
                    {
                        new { Name = "Phone", Quantity = 1 },
                        new { Name = "Charger", Quantity = 2 }
                    }
                },
                new
                {
                    Id = 102,
                    Title = "Groceries",
                    IsPaid = false,
                    Items = new[]
                    {
                        new { Name = "Apple", Quantity = 6 },
                        new { Name = "Milk", Quantity = 2 }
                    }
                }
            },
                    Notifications = new[] { "Welcome back!", "Your order #101 was shipped." },
                    PromoCode = "DISCOUNT10"
                }
            };

            string expected = "<h1>Alex's Dashboard</h1><p>Status: Active</p><p>Age: 32</p><h2>Orders:</h2><ul><li><b>Order #101</b> - Electronics<span>(Paid)</span><ul><li>Phone x 1</li><li>Charger x 2</li></ul></li><li><b>Order #102</b> - Groceries<span>(Pending)</span><ul><li>Apple x 6</li><li>Milk x 2</li></ul></li></ul><h2>Notifications:</h2><ul><li>Welcome back!</li><li>Your order #101 was shipped.</li></ul><p>Your promo code: DISCOUNT10</p><footer>Generated for Alex</footer>";

            // act
            string result = renderer.RenderFromString(template, model);

            // normalize (удаляем пробелы и переносы для сравнения)
            string normalize(string s) => s.Replace("\r", "").Replace("\n", "").Replace(" ", "");
            // assert
            Assert.AreEqual(normalize(expected), normalize(result));
        }

        [TestClass]
        public class HtmlTemplateRendererTests
        {
            /// <summary>
            /// Нормализует строку для сравнения, убирая разрывы строк и пробелы.
            /// </summary>
            private static string Normalize(string s)
            {
                return s.Replace("\r", string.Empty)
                        .Replace("\n", string.Empty)
                        .Replace(" ", string.Empty);
            }

            /// <summary>
            /// Проверяет корректность обработки конструкций $if/$else и вставки переменных.
            /// </summary>
            /// <param name="template">Шаблон для рендеринга.</param>
            /// <param name="expected">Ожидаемый результат после рендеринга.</param>
            [DataTestMethod]
            [DataRow("$if(IsTrue) <p>User is active</p>$endif", " <p>User is active</p>")]
            [DataRow("$if(IsTrue) <p>User is active</p>$else <p>Not active</p>$endif", " <p>User is active</p>")]
            [DataRow("$if(IsTrue) <p>${Name}</p>$endif", " <p>Aboba</p>")]
            [DataRow("${Email} $if(IsTrue) <p>${Name}</p>$endif", "test@test.ru  <p>Aboba</p>")]
            [DataRow("$if(IsTrue) <p>${Name}</p>$endif ${Email}", " <p>Aboba</p> test@test.ru")]
            [DataRow("$if(IsFalse) <p>User is active</p>$endif ", " ")]
            [DataRow("$if(IsFalse) <p>User is active</p>$else <p>Not active</p>$endif", " <p>Not active</p>")]
            [DataRow("$if(IsFalse) <p>User is active</p>$else <p>${Name}</p>$endif", " <p>Aboba</p>")]
            [DataRow("$if(IsTrue)$if(IsTrue)${Name}$endif $endif", "Aboba ")]
            [DataRow("$if(IsTrue)$if(IsTrue)${Name}$else Net $endif $endif", "Aboba ")]
            [DataRow("$if(IsTrue)$if(IsFalse)${Name}$else Net $endif $endif", " Net  ")]
            [DataRow("$if(IsFalse)$if(IsFalse)${Name}$else Net $endif $endif", "")]
            [DataRow("$if(IsFalse)$if(IsFalse)${Name}$else Net $endif $else a $endif", " a ")]
            [DataRow("$if(IsTrue)${Email} $if(IsFalse)${Name}$else Net $endif $else a $endif", "test@test.ru  Net  ")]
            public void TestIfAndVariableBlocks(string template, string expected)
            {
                // arrange
                HtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
                var model = new
                {
                    Name = "Aboba",
                    Email = "test@test.ru",
                    Group = new
                    {
                        Id = 1,
                        Name = "11-409",
                    },
                    IsTrue = true,
                    IsFalse = false,
                };

                // act
                string result = renderer.RenderFromString(template, model);

                // assert (нормализуем строку для корректного сравнения)
                Assert.AreEqual(Normalize(expected), Normalize(result));
            }
        }
    }
}
