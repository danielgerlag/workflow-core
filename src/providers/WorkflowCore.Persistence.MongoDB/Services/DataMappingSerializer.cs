using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Models;
using WorkflowCore.Persistence.MongoDB.Models;

namespace WorkflowCore.Persistence.MongoDB.Services
{
    public class DataMappingSerializer : SerializerBase<DataMapping>
    {
        public override DataMapping Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {                        
            var raw = BsonSerializer.Deserialize<PersistedMapping>(context.Reader);
            var result = new DataMapping();

            var sourceParameterType = Type.GetType(raw.SourceParameterType);
            var sourceReturnType = Type.GetType(raw.SourceReturnType);
            var targetParameterType = Type.GetType(raw.TargetParameterType);
            var targetReturnType = Type.GetType(raw.TargetReturnType);

            result.Source = ParseExpression(raw.SourceExpression, sourceParameterType, sourceReturnType);
            result.Target = ParseExpression(raw.TargetExpression, targetParameterType, targetReturnType);

            return result;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DataMapping value)
        {
            var valueObj = new PersistedMapping();
            valueObj.SourceParameterType = value.Source.Parameters.Single().Type.AssemblyQualifiedName;
            valueObj.SourceExpression = value.Source.ToString();
            valueObj.SourceReturnType = value.Source.ReturnType.AssemblyQualifiedName;

            valueObj.TargetParameterType = value.Target.Parameters.Single().Type.AssemblyQualifiedName;
            valueObj.TargetExpression = value.Target.ToString();
            valueObj.TargetReturnType = value.Target.ReturnType.AssemblyQualifiedName;

            BsonSerializer.Serialize(context.Writer, valueObj);
        }
        
        private LambdaExpression ParseExpression(string expression, Type parameterType, Type returnType)
        {
            var split = expression.Split(new string[] { "=>" }, StringSplitOptions.None);
            var paramName = split[0].Trim();
            var body = split[1].Trim();
            var p1 = Expression.Parameter(parameterType, paramName);
            List<ParameterExpression> p = new List<ParameterExpression>();
            p.Add(p1);
            var result = DynamicExpressionParser.ParseLambda(true, p.ToArray(), returnType, body);
            return result;
        }
                
    }
}
