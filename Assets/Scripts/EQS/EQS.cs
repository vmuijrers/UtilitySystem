using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EQS : MonoBehaviour
{
    public GameObject target;
    public int numPointsPerRing = 10;
    public int numRings = 3;
    public float radiusPerRing = 5;
    private List<EQSPoint> points = new List<EQSPoint>();
    private IQueryStrategy strategy;
    private NavMeshAgent agent;
    public float maxQueryDistance = 50;

    public class EQSPoint : IEvaluatable
    {
        public Vector3 point;
        public float value;

        public EQSPoint(Vector3 point)
        {
            this.point = point;
            value = 0;
        }

        public float Evaluate(IQueryStrategy strategy)
        {
            value = strategy.Query(point);
            Debug.Log($"value: {value}");
            return value;
        }
    }

    public interface IEvaluatable
    {
        float Evaluate(IQueryStrategy strategy);
    }

    public interface IQueryStrategy
    {
        float Query(Vector3 point);
    }

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        //strategy = new DistanceQuery(gameObject, maxQueryDistance);
        strategy = new LineOfSightQuery(target);
        points = new List<EQSPoint>();

        float stepAngle = 360f / numPointsPerRing;
        for (int i = 0; i < numRings; i++)
        {
            for (int j = 0; j < numPointsPerRing; j++)
            {
                float angle = j * stepAngle * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).normalized * radiusPerRing * (i + 1);
                var point = new EQSPoint(target.transform.position + p);
                points.Add(point);
            }
        }
    }

    public class DistanceQuery : IQueryStrategy
    {
        private GameObject target;
        private float maxRange = 100;

        public DistanceQuery(GameObject target, float maxRange)
        {
            this.target = target;
            this.maxRange = maxRange;
        }

        public float Query(Vector3 point)
        {
            return Vector3.Distance(point, target.transform.position) / maxRange;
        }
    }

    public class LineOfSightQuery : IQueryStrategy
    {
        private GameObject target;
        
        public LineOfSightQuery(GameObject target)
        {
            this.target = target;
        }

        public float Query(Vector3 point)
        {
            Vector3 pos = point + new Vector3(0, 1.6f, 0);
            Vector3 dir = target.transform.position - pos;
            if(Physics.Raycast(pos, dir.normalized, out RaycastHit hit, 1000))
            {
                return hit.collider.gameObject == target.gameObject ? 1 : 0;
            }
            return 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if((!agent.hasPath && !agent.pathPending) || agent.remainingDistance < 1f)
        {
            UpdateTargetPosition();
        }
    }

    public void UpdateTargetPosition()
    {
        var bestPoint = QueryEnvironment(target.transform.position, numRings, numPointsPerRing, radiusPerRing, strategy);
        if (bestPoint != null)
        {
            agent.SetDestination(bestPoint.point);
        }
    }

    public EQSPoint QueryEnvironment(Vector3 targetPosition, int numRings, int pointsPerRing, float radiusPerRing, IQueryStrategy strategy)
    {
        float stepAngle = 360f / pointsPerRing;
        for(int i = 0; i < numRings; i++)
        {
            for(int j = 0; j < pointsPerRing; j++)
            {
                float angle = j * stepAngle * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).normalized * radiusPerRing * (i+1);
                points[j + i * pointsPerRing].point = targetPosition + p;
                points[j + i * pointsPerRing].Evaluate(strategy);
            }
        }

        return points.OrderByDescending(x => x.value).FirstOrDefault();
    }

    private void OnDrawGizmos()
    {
        foreach(EQSPoint p in points)
        {
            Gizmos.color = Color.Lerp(Color.red, Color.green, p.value);
            Gizmos.DrawWireSphere(p.point, 1f);
        }
    }
}
