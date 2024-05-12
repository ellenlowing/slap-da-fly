using System.Collections;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class FlyMovement : MonoBehaviour
{
    public MRUKAnchor.SceneLabels canLand = MRUKAnchor.SceneLabels.FLOOR | MRUKAnchor.SceneLabels.CEILING;
    public float minRestDuration = 2f;
    public float maxRestDuration = 5f;
    public float speed = 2.0f;
    public float rotationSpeed = 10f;
    public float minDistanceForNewTarget = 5.0f;
    public float distanceToEdges = 0.2f;

    private bool isResting = false;
    private Vector3 targetPosition;
    private Vector3 targetNormal;
    private bool needNewTarget = true;

    private void Start()
    {
    }

    private void Update()
    {
        if (needNewTarget)
        {
            FindNewPosition();
        }
        
        if (!isResting && !needNewTarget)
        {
            MoveTowardsTargetPosition();
        }
        
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            StartCoroutine(Rest());
        }
    }

    private void MoveTowardsTargetPosition()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
    }

    private void FindNewPosition()
    {
        MRUKRoom currentRoom = MRUK.Instance.GetCurrentRoom();
        if (currentRoom != null)
        {
            var labelFilter = LabelFilter.FromEnum(canLand);

            if (currentRoom.GenerateRandomPositionOnSurface(MRUK.SurfaceType.FACING_UP | MRUK.SurfaceType.VERTICAL,
                distanceToEdges, labelFilter, out Vector3 position, out Vector3 normal))
            {
                Vector3 direction = (position - transform.position).normalized;
                if (!Physics.Raycast(transform.position + direction * 0.5f, direction, Vector3.Distance(transform.position, position) - 0.5f))
                {
                    targetPosition = position;
                    targetNormal = normal;
                    needNewTarget = false;
                }
            }
        }
    }

    private IEnumerator Rest()
    {
        isResting = true;
        RaycastHit hit;
        if (Physics.Raycast(transform.position + targetNormal * 0.1f, -targetNormal, out hit, 0.2f))
        {
            transform.position = hit.point;
            Vector3 forwardDirection = Vector3.Cross(transform.right, targetNormal);
            transform.rotation = Quaternion.LookRotation(forwardDirection, targetNormal);
        }
        else
        {
            transform.up = targetNormal;
            transform.rotation = Quaternion.LookRotation(Vector3.Cross(transform.right, targetNormal), targetNormal);
        }
        yield return new WaitForSeconds(Random.Range(minRestDuration, maxRestDuration));
        isResting = false;
        needNewTarget = true;
    }
}
    